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

### macOS Developer Workflow

#### Initial Setup on macOS

```bash
brew install --cask rancher-desktop
brew install docker docker-compose
```

#### Switch Rancher Desktop to dockerd

Open Rancher Desktop → Preferences → Kubernetes → Container Runtime:
```
✔ dockerd
❌ containerd
```

#### Build the project

```bash
./scripts/build.sh
```

#### Run everything

```bash
./scripts/run.sh
```

#### Stop everything

```bash
./scripts/stop.sh
```

#### View logs

```bash
./scripts/logs.sh api
./scripts/logs.sh worker
./scripts/logs.sh postgres
./scripts/logs.sh rabbitmq
```

### Scripts for macOS + Rancher Desktop

All scripts under:

```bash
scripts/
    build.sh
    run.sh
    stop.sh
    clean.sh
    logs.sh
```

Each script is multilayered to support:
Docker Desktop
Rancher Desktop (containerd or dockerd mode)

Example for run.sh:

```bash
#!/bin/bash
set -e
echo "Starting full stack (API + Worker + PostgreSQL + RabbitMQ)..."
docker compose up -d --build
```

Example for build.sh:

```bash
#!/bin/bash
set -e

echo "Building containers..."
docker compose build
```

Example for stop.sh:

```bash
#!/bin/bash
docker compose down
```

Example for clean.sh:

```bash
#!/bin/bash
docker compose down -v
docker system prune -af
