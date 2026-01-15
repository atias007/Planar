# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Planar is an enterprise job scheduler service built on Quartz.NET. It provides infrastructure for scheduling and managing jobs with features like monitoring, clustering, hooks, and a REST API.

## Build Commands

```bash
# Build main solution (from src/)
dotnet build src/Planar.sln

# Build release
dotnet build src/Planar.sln -c Release

# Run the service
dotnet run --project src/Planar/Planar.csproj

# Run tests (NUnit)
dotnet test src/Planar.Test/Planar.Test.csproj

# Build Docker image (from src/)
docker build -f src/Dockerfile -t planar src/

# Build generic jobs solution
dotnet build "generic jobs/GenericJobs.sln"

# Build NuGet packages solution
dotnet build "nuget packages/Planar.Packages.sln"
```

## Architecture

### Solution Structure

- **src/Planar** - ASP.NET Core Web API host with REST controllers
- **src/Planar.Service** - Core scheduler service with Quartz.NET integration, business logic (API domains), Entity Framework data layer, and background services
- **src/Planar.CLI** - Command-line interface tool for managing Planar
- **src/Planar.Common** - Shared utilities and constants
- **src/Planar.API.Common** - Shared API models and DTOs

### Job Types (src/Jobs/)

Built-in job types that run within the scheduler:
- **PlanarJob** - Base job for custom .NET jobs
- **ProcessJob** - Execute external processes
- **SqlJob** - Run SQL scripts
- **RestJob** - Make HTTP REST calls
- **PowerShellJob** - Execute PowerShell scripts
- **SequenceJob** - Chain multiple jobs
- **SqlTableReportJob** - Generate reports from SQL

### Generic Jobs (generic jobs/)

Standalone job implementations for common tasks (health checks, folder operations, service monitoring, Redis/RabbitMQ operations, etc.). These are separate deployable job packages.

### NuGet Packages (nuget packages/)

Published packages for external job/hook development:
- **Planar.Job** - Library for writing custom .NET jobs
- **Planar.Hook** - Library for writing monitor hooks
- **Planar.Client** - Client library for Planar API
- **Planar.Job.Test** / **Planar.Hook.Test** - Testing utilities

### API Layer Pattern

Controllers in `src/Planar/Controllers/` delegate to domain classes in `src/Planar.Service/API/`:
- Controllers handle HTTP routing and response formatting
- Domain classes (e.g., `JobDomain`, `TriggerDomain`, `MonitorDomain`) contain business logic
- Data layer in `src/Planar.Service/Data/` uses Entity Framework Core

### Database Support

Supports multiple providers configured in AppSettings.yml:
- SQLite (default for development)
- SQL Server
- PostgreSQL
- MySQL

Database migrations are in `src/DatabaseMigrations/` with provider-specific scripts.

## Configuration

Settings are in YAML files under `src/Planar/Data/Settings/`:
- `AppSettings.yml` - Main configuration (ports, database, clustering, authentication, hooks)
- `Serilog.yml` - Logging configuration
- `WorkingHours.yml` - Calendar/schedule constraints

## Code Style

- .NET 9.0, C# with nullable enabled
- Follow standard .NET/ASP.NET Core conventions
- Use async/await for I/O operations
- Dependency injection via constructor injection
- FluentValidation for request validation
- AutoMapper and Riok.Mapperly for object mapping
