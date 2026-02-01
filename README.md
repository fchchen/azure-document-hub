# Azure Document Hub

A full-stack document management application demonstrating Azure cloud services integration with .NET 8 and Angular.

## Tech Stack

### Backend
- **.NET 8** - ASP.NET Core Web API with minimal APIs
- **Azure Functions** (.NET Isolated Worker) - Serverless document processing
- **Azure Cosmos DB** - NoSQL database for document metadata
- **Azure Blob Storage** - Document file storage
- **Azure Queue Storage** - Message queue for async processing

### Frontend
- **Angular 18+** - Modern standalone components with signals
- **Angular Material** - UI component library
- **TypeScript** - Type-safe development

## Architecture

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  Angular SPA    │────▶│   .NET 8 API     │────▶│  Cosmos DB      │
│  (Frontend)     │     │                  │     │  (Metadata)     │
└─────────────────┘     └────────┬─────────┘     └─────────────────┘
                                 │
                                 ▼
                        ┌────────────────┐
                        │  Blob Storage  │
                        │  (Documents)   │
                        └────────┬───────┘
                                 │
                                 ▼
                        ┌────────────────┐
                        │ Queue Storage  │
                        └────────┬───────┘
                                 │
                                 ▼
                        ┌────────────────┐
                        │Azure Functions │
                        │(Processing)    │
                        └────────────────┘
```

## Features

- **Document Upload** - Drag & drop file upload with validation
- **Async Processing** - Azure Functions process documents via queue trigger
- **Status Tracking** - Real-time document processing status
- **Secure Downloads** - SAS token-based secure file downloads
- **Responsive UI** - Mobile-friendly Angular Material design

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Azurite](https://docs.microsoft.com/azure/storage/common/storage-use-azurite) (for local storage emulation)
- [Azure Cosmos DB Emulator](https://docs.microsoft.com/azure/cosmos-db/local-emulator) (for local Cosmos DB)

## Local Development

### 1. Start Local Emulators

```bash
# Start Azurite (Azure Storage Emulator)
azurite --silent --location ./azurite --debug ./azurite/debug.log

# Start Cosmos DB Emulator (Windows) or use the Docker image
```

### 2. Run Backend API

```bash
cd src/Api
dotnet run
```

API will be available at `http://localhost:5000`

### 3. Run Azure Functions

```bash
cd src/Functions
func start
```

Functions will be available at `http://localhost:7071`

### 4. Run Angular Frontend

```bash
cd frontend
npm install
npm start
```

Frontend will be available at `http://localhost:4200`

## Project Structure

```
azure-document-hub/
├── src/
│   ├── Api/                    # ASP.NET Core Web API
│   │   ├── Endpoints/          # Minimal API endpoints
│   │   ├── Services/           # Business logic services
│   │   └── Program.cs          # Application entry point
│   ├── Functions/              # Azure Functions
│   │   └── Functions/          # Function implementations
│   └── Shared/                 # Shared models and DTOs
│       ├── Models/
│       ├── DTOs/
│       └── Constants/
├── tests/
│   └── Api.Tests/              # Unit tests
├── frontend/                   # Angular application
│   └── src/
│       └── app/
│           ├── components/     # UI components
│           ├── services/       # API services
│           └── models/         # TypeScript interfaces
└── infrastructure/             # Azure Bicep templates
```

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/documents` | Upload a document |
| GET | `/api/documents` | List all documents (paginated) |
| GET | `/api/documents/{id}` | Get document details |
| GET | `/api/documents/{id}/download` | Get secure download URL |
| DELETE | `/api/documents/{id}` | Delete a document |

## Deployment to Azure

### Using Azure CLI

```bash
# Login to Azure
az login

# Create resource group
az group create --name rg-document-hub --location eastus

# Deploy infrastructure
az deployment group create \
  --resource-group rg-document-hub \
  --template-file infrastructure/main.bicep \
  --parameters environment=dev
```

### Deploy Applications

```bash
# Build and deploy API
cd src/Api
dotnet publish -c Release
az webapp deploy --resource-group rg-document-hub --name dochub-api-dev --src-path ./bin/Release/net8.0/publish

# Build and deploy Functions
cd src/Functions
func azure functionapp publish dochub-func-dev

# Build and deploy Frontend
cd frontend
npm run build
# Deploy to Static Web Apps via GitHub Actions or Azure CLI
```

## Testing

```bash
# Run unit tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## License

MIT
