# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Run Commands

```bash
# Build entire solution
dotnet build

# Run API (from project root)
dotnet run --project src/Api/DocumentHub.Api.csproj

# Run Azure Functions locally
cd src/Functions && func start

# Run Angular frontend
cd frontend && npm start

# Run tests
dotnet test

# Run single test file
dotnet test --filter "FullyQualifiedName~DocumentServiceTests"
```

## Architecture Overview

This is a document processing application using Azure services:

- **src/Api** - ASP.NET Core 8 Web API using minimal APIs pattern
  - `Endpoints/` - API route handlers
  - `Services/` - Business logic with interface abstractions (IBlobStorageService, ICosmosDbService, IQueueService, IDocumentService)

- **src/Functions** - Azure Functions (isolated worker model) for async document processing
  - Queue-triggered function processes uploaded documents

- **src/Shared** - Shared library containing models, DTOs, and constants used by both API and Functions

- **frontend** - Angular 18+ standalone components application
  - Uses signals for state management
  - Angular Material for UI components

## Key Integration Points

1. **Document Upload Flow**: API → Blob Storage → Queue Message → Azure Function → Cosmos DB update
2. **Cosmos DB**: Uses `/id` as partition key with camelCase property naming
3. **Storage**: Uses Azurite connection string `UseDevelopmentStorage=true` for local dev
4. **CORS**: API allows `http://localhost:4200` for Angular dev server

## Testing Patterns

- Services use interface abstractions for mockability
- Integration tests use `WebApplicationFactory<Program>`
- Test project references main API project for integration testing
