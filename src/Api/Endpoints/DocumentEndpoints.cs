using DocumentHub.Api.Services;
using DocumentHub.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace DocumentHub.Api.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents")
            .WithTags("Documents");

        group.MapPost("/", UploadDocument)
            .DisableAntiforgery()
            .Produces<DocumentUploadResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/", GetDocuments)
            .Produces<DocumentListResponse>();

        group.MapGet("/{id}", GetDocument)
            .Produces<DocumentResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/download", GetDownloadUrl)
            .Produces<string>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}", DeleteDocument)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> UploadDocument(
        IFormFile file,
        [FromHeader(Name = "X-User-Id")] string? userId,
        IDocumentService documentService)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest("No file provided");
        }

        var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png", "image/gif",
            "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };

        if (!allowedTypes.Contains(file.ContentType))
        {
            return Results.BadRequest($"File type {file.ContentType} is not allowed");
        }

        const long maxSize = 50 * 1024 * 1024; // 50MB
        if (file.Length > maxSize)
        {
            return Results.BadRequest("File size exceeds 50MB limit");
        }

        using var stream = file.OpenReadStream();
        var result = await documentService.UploadDocumentAsync(
            stream,
            file.FileName,
            file.ContentType,
            userId ?? "anonymous");

        return Results.Created($"/api/documents/{result.Id}", result);
    }

    private static async Task<IResult> GetDocuments(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IDocumentService documentService)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize > 100 ? 100 : pageSize;

        var result = await documentService.GetDocumentsAsync(page, pageSize);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetDocument(string id, IDocumentService documentService)
    {
        var document = await documentService.GetDocumentAsync(id);
        return document is null ? Results.NotFound() : Results.Ok(document);
    }

    private static async Task<IResult> GetDownloadUrl(
        string id,
        [FromQuery] int? expiresInMinutes,
        IDocumentService documentService)
    {
        try
        {
            var expiry = expiresInMinutes.HasValue
                ? TimeSpan.FromMinutes(expiresInMinutes.Value)
                : TimeSpan.FromHours(1);

            var url = await documentService.GetDownloadUrlAsync(id, expiry);
            return Results.Ok(new { downloadUrl = url });
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> DeleteDocument(string id, IDocumentService documentService)
    {
        try
        {
            await documentService.DeleteDocumentAsync(id);
            return Results.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
