# PdfReaderService

Simple .NET 9-based PDF reader/processing service with:
- API (upload, status, result)
- Worker (async processing via queue)
- PostgreSQL for metadata
- RabbitMQ for queueing
- File system storage for PDFs + JSON
- Docker + docker-compose for local/Rancher Desktop

## Prerequisites

- macOS
- Rancher Desktop (recommended) or Docker Desktop
- .NET 9 SDK (when available) if you want to run without containers

## Using Docker (recommended)

From the repository root:

```bash
# Build images
./scripts/build.sh

# Start stack (Postgres + RabbitMQ + API + Worker)
./scripts/run.sh

# Check logs
./scripts/logs.sh api
./scripts/logs.sh worker

# Stop stack
./scripts/stop.sh
```

The API will be available at: `http://localhost:8080`

Swagger UI: `http://localhost:8080/swagger`

## Endpoints

- `POST /documents`  
  - Content-Type: `multipart/form-data`  
  - Fields:
    - `file`: PDF file to upload
    - `formType` (optional): known form type / schema name

- `GET /documents/{id}`  
  - Returns basic metadata and status.

- `GET /documents/{id}/result`  
  - Returns JSON result when ready, `202 Accepted` while processing.

## Development (VS Code / Windsurf)

### VS Code

- Open the folder in VS Code.
- Recommended extensions:
  - C# Dev Kit
  - Docker
- Use task: **build** (Ctrl+Shift+B).
- Use launch configuration: **Launch PdfReader.Api**.

### Windsurf

- Open `.windsurf/workspace.code-workspace`.
- Default solution: `PdfReaderService.sln`.
- Use your preferred run/debug configuration.
