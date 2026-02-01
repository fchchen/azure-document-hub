using DocumentHub.Api.Services;
using DocumentHub.Shared.Constants;
using DocumentHub.Shared.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocumentHub.Api.Tests;

public class DocumentServiceTests
{
    private readonly Mock<IBlobStorageService> _blobStorageMock;
    private readonly Mock<ICosmosDbService> _cosmosDbMock;
    private readonly Mock<IQueueService> _queueServiceMock;
    private readonly Mock<ILogger<DocumentService>> _loggerMock;
    private readonly DocumentService _sut;

    public DocumentServiceTests()
    {
        _blobStorageMock = new Mock<IBlobStorageService>();
        _cosmosDbMock = new Mock<ICosmosDbService>();
        _queueServiceMock = new Mock<IQueueService>();
        _loggerMock = new Mock<ILogger<DocumentService>>();

        _sut = new DocumentService(
            _blobStorageMock.Object,
            _cosmosDbMock.Object,
            _queueServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task UploadDocumentAsync_ShouldUploadToBlobAndSaveMetadata()
    {
        // Arrange
        var fileName = "test.pdf";
        var contentType = "application/pdf";
        var uploadedBy = "testuser";
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        _blobStorageMock
            .Setup(x => x.UploadAsync(
                AzureConstants.DocumentsContainer,
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                contentType))
            .ReturnsAsync("https://storage.blob.core.windows.net/documents/test.pdf");

        _cosmosDbMock
            .Setup(x => x.CreateDocumentAsync(It.IsAny<Document>()))
            .ReturnsAsync((Document doc) => doc);

        // Act
        var result = await _sut.UploadDocumentAsync(stream, fileName, contentType, uploadedBy);

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be(fileName);
        result.Status.Should().Be("Pending");

        _blobStorageMock.Verify(x => x.UploadAsync(
            AzureConstants.DocumentsContainer,
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            contentType), Times.Once);

        _cosmosDbMock.Verify(x => x.CreateDocumentAsync(
            It.Is<Document>(d => d.OriginalFileName == fileName && d.UploadedBy == uploadedBy)),
            Times.Once);

        _queueServiceMock.Verify(x => x.SendMessageAsync(
            AzureConstants.ProcessingQueue,
            It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDocumentAsync_WhenDocumentExists_ShouldReturnDocument()
    {
        // Arrange
        var documentId = "test-id";
        var document = new Document
        {
            Id = documentId,
            OriginalFileName = "test.pdf",
            ContentType = "application/pdf",
            Status = DocumentStatus.Completed
        };

        _cosmosDbMock
            .Setup(x => x.GetDocumentAsync(documentId))
            .ReturnsAsync(document);

        // Act
        var result = await _sut.GetDocumentAsync(documentId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(documentId);
        result.FileName.Should().Be("test.pdf");
    }

    [Fact]
    public async Task GetDocumentAsync_WhenDocumentNotFound_ShouldReturnNull()
    {
        // Arrange
        var documentId = "non-existent-id";

        _cosmosDbMock
            .Setup(x => x.GetDocumentAsync(documentId))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _sut.GetDocumentAsync(documentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteDocumentAsync_ShouldDeleteFromBlobAndCosmosDb()
    {
        // Arrange
        var documentId = "test-id";
        var document = new Document
        {
            Id = documentId,
            FileName = "stored-name.pdf",
            OriginalFileName = "test.pdf"
        };

        _cosmosDbMock
            .Setup(x => x.GetDocumentAsync(documentId))
            .ReturnsAsync(document);

        _blobStorageMock
            .Setup(x => x.DeleteAsync(AzureConstants.DocumentsContainer, document.FileName))
            .ReturnsAsync(true);

        // Act
        await _sut.DeleteDocumentAsync(documentId);

        // Assert
        _blobStorageMock.Verify(x => x.DeleteAsync(
            AzureConstants.DocumentsContainer,
            document.FileName), Times.Once);

        _cosmosDbMock.Verify(x => x.DeleteDocumentAsync(documentId), Times.Once);
    }
}
