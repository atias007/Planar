# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Planar is an enterprise job scheduler service built on Quartz.NET. It provides infrastructure for scheduling and managing jobs with features like monitoring, clustering, hooks, and a REST API.

## Build Commands

```bash
# Build main solution (from repo root)
dotnet build src/Planar.sln

# Build release
dotnet build src/Planar.sln -c Release

# Run the service
dotnet run --project src/Planar/Planar.csproj

# Run the CLI
dotnet run --project src/Planar.CLI/Planar.CLI.csproj

# Run tests (NUnit)
dotnet test src/Planar.Test/Planar.Test.csproj

# Build Docker image (from src/)
docker build -f src/Dockerfile -t planar src/

# Build generic jobs solution (standalone deployable job packages)
dotnet build "generic jobs/GenericJobs.sln"

# Build NuGet packages solution (Planar.Job, Planar.Hook, Planar.Client, etc.)
dotnet build "nuget packages/Planar.Packages.sln"
```

Target framework is `net10.0` across the main solution. Nullable reference types are enabled.

Note: `src/Planar.Test/JobTests.cs` is currently excluded from compilation (`<Compile Remove>` + re-included as `<None>` in the csproj), so `dotnet test` runs zero tests against a clean checkout. Check whether this is intentional before assuming test coverage exists for a change.

There is also a `test/` directory at the repo root (`test/Planar.Test.sln`) — despite the name, this is a solution of demo/scratch job and hook projects (e.g. `HelloWorld`, `LongRunningJob`, `DemoHook`) used for manual/local testing against a running Planar instance, not an automated test suite.

## Architecture

### Solution Structure

- **src/Planar** - ASP.NET Core Web API host with REST controllers
- **src/Planar.Service** - Core scheduler service with Quartz.NET integration, business logic (API domains), Entity Framework data layer, and background services
- **src/Planar.CLI** - Command-line interface tool for managing Planar (`planar-cli`)
- **src/Planar.Common** - Shared utilities, config models (`AppSettings/`), and constants
- **src/Planar.API.Common** - Shared API request/response DTOs used by both the API host and the CLI/clients
- **src/Hooks** (`Planar.Hooks`) - Built-in monitor hook implementations (email/SMTP, Teams, Telegram, Twilio SMS, REST, Redis pub/sub & streams, RabbitMQ, log)
- **src/Planar.Watcher** - Companion Windows service/watchdog process for the main Planar service
- **src/DatabaseMigrations** / **src/DatabaseMigrations.Factory** / **src/DatabaseMigrations.Scripts** - DbUp-based migration runner and provider-specific SQL scripts

### Job Types (src/Jobs/)

Built-in job types that run within the scheduler:
- **PlanarJob** / **PlanarRemoteJob** - Base jobs for custom .NET jobs (in-process / remote)
- **ProcessJob** - Execute external processes
- **SqlJob** - Run SQL scripts
- **RestJob** - Make HTTP REST calls
- **PowerShellJob** - Execute PowerShell scripts
- **SequenceJob** - Chain multiple jobs
- **SqlTableReportJob** - Generate reports from SQL
- **CommonJob** - Shared base classes/properties (e.g. `BaseProperties.cs`) used across the job type implementations above

### Generic Jobs (generic jobs/)

Standalone deployable job packages for common operational tasks: health checks, folder operations (check/retention/sync), Windows service check/restart, IIS app pool recycle, and Redis/RabbitMQ/InfluxDB/SQL query checks. Each is built and published independently of the main solution.

### NuGet Packages (nuget packages/)

Published packages for external job/hook development and API consumption:
- **Planar.Job** / **Planar.Job.Http** / **Planar.Job.RabbitMQ** - Libraries for writing custom .NET jobs
- **Planar.Hook** - Library for writing custom monitor hooks
- **Planar.Client** - Client library for the Planar REST API
- **Planar.Job.Test** / **Planar.Hook.Test** - Testing utilities for job/hook authors
- **Planar.Common** - Shared package used by the above (published subset of `src/Planar.Common`)

### API Layer Pattern

Controllers in `src/Planar/Controllers/` delegate to domain classes in `src/Planar.Service/API/`:
- Controllers handle HTTP routing and response formatting
- Domain classes (e.g. `JobDomain`, `TriggerDomain`, `MonitorDomain`, `ConfigDomain`, `ClusterDomain`, `UserDomain`, `GroupDomain`) contain business logic, extending `BaseBL`/`BaseJobBL`/`BaseLazyBL`
- Large domains are split across partial classes by concern (e.g. `JobDomainAddPartial`, `JobDomainUpdatePartial`, `JobDomainComparePartial`, `JobDomainSse`, `JobDomainWait`)
- Data layer in `src/Planar.Service/Data/` uses Entity Framework Core (`PlanarContext`), one data class per aggregate (e.g. `JobData`, `MonitorData`, `HistoryData`)
- Request validation lives in `src/Planar.Service/Validation/` using FluentValidation, one validator per request DTO

### Database Support

Supports multiple providers configured in AppSettings.yml:
- SQLite (default for development)
- SQL Server
- PostgreSQL
- MySQL

Database migrations are DbUp-driven (`src/DatabaseMigrations`), with provider-specific SQL scripts under `src/DatabaseMigrations.Scripts`.

## Configuration

Settings are in YAML files under `src/Planar/Data/Settings/`:
- `AppSettings.yml` - Main configuration (ports, database, clustering, authentication, hooks)
- `Serilog.yml` - Logging configuration
- `WorkingHours.yml` / `WorkingHours.Israel.yml` - Calendar/schedule constraints

## Code Style

- .NET 10.0, C# with nullable enabled
- Follow standard .NET/ASP.NET Core conventions
- Use async/await for I/O operations
- Dependency injection via constructor injection
- FluentValidation for request validation
- AutoMapper and Riok.Mapperly for object mapping
- PascalCase for classes/methods/public members, camelCase for locals/private fields, `I`-prefix for interfaces
