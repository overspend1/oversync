# OverSync Replatform

This directory contains the WinUI 3 / .NET replatformed stack:

- `src/OverSync.Windows`: Windows desktop client (WinUI 3, MVVM).
- `src/OverSync.Core`: portable sync engine (watcher, AES-GCM encryption, chunking, SQLite state).
- `src/OverSync.Contracts`: shared DTOs and API contracts.
- `src/OverSync.Api`: ASP.NET Core API (`/v1/auth`, `/v1/devices`, `/v1/sync`).
- `src/OverSync.Infrastructure`: metadata/chunk persistence (in-memory, PostgreSQL, local file/S3-compatible).
- `tests/OverSync.Tests`: unit and API integration tests.

## Quick Start

1. Build solution:

```powershell
dotnet build .\OverSync.Replatform.sln
```

2. Run API (in-memory mode by default):

```powershell
dotnet run --project .\src\OverSync.Api
```

3. Run Windows app:

```powershell
dotnet run --project .\src\OverSync.Windows
```

## PostgreSQL + MinIO

Start dev services:

```powershell
docker compose up -d
```

Then set `Infrastructure.UseInMemory=false` and `Infrastructure.Storage.UseS3=true` in `src/OverSync.Api/appsettings.json`.
