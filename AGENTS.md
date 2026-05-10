# AGENTS.md

## Project Overview

.NET 10 Clean Architecture web application for tracking ARK fund holdings.
PostgreSQL database with dual persistence: Marten (document store for holdings) and EF Core (Identity for users).

## Architecture

```
Domain          Pure business models and services. No external dependencies.
Application     Services, interfaces, DTOs, clients. Depends on Domain.
Infrastructure  Repositories, cron jobs, Identity, DI registration. Depends on Application + Domain.
Presentation    ASP.NET Core MVC controllers and views. Depends on Application + Infrastructure.
Tests           Single test project covering all layers.
```

## Build & Run

```bash
# Prerequisites: .NET 10, Docker/Podman (for PostgreSQL)
docker compose up -d                                         # start PostgreSQL
dotnet build PV260Project.sln                                # Debug (local dev)
dotnet build PV260Project.sln --configuration Release
dotnet run --project Presentation/Presentation.csproj
```

CI builds and tests use `--configuration Release`. Verify locally with Release before pushing.

## Testing

See `TESTING_GUIDELINES.md` for full conventions. Key points:

- **Framework:** xUnit + Shouldly assertions + Moq mocking
- **Naming:** `MethodName_ExpectedBehavior_WhenCondition`
- **Setup:** Factory methods returning `(SUT, Mock1, Mock2, ...)` tuples
- **No `using Xunit;`** — globally imported via Tests.csproj
- **Structure:** Arrange-Act-Assert separated by blank lines, no section comments

### Layer-specific test strategies

| Layer | Strategy |
| --- | --- |
| Domain | Pure unit tests, no mocking |
| Application Services | Unit tests with Moq |
| Application Clients | Integration tests with WireMock.Net |
| Infrastructure Repos (Marten) | Integration tests with Testcontainers (`IClassFixture<PostgresFixture>`) |
| Infrastructure Repos (Identity) | Unit tests with Moq for `UserManager` |
| Presentation Controllers | Unit tests with Moq |

### Running tests

```bash
dotnet test Tests/Tests.csproj                                                  # all tests (needs Docker)
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName!~HoldingsRepository" # skip Testcontainers tests (no Docker needed)
```

For Podman, start the socket first:

```bash
systemctl --user start podman.socket
DOCKER_HOST="unix:///run/user/$(id -u)/podman/podman.sock" dotnet test Tests/Tests.csproj
```

## CI Pipeline

GitLab CI (`.gitlab-ci.yml`): build and test stages on `shared-fi` runners.
Integration tests requiring Docker are filtered out in CI because the shared runners lack Docker-in-Docker support.

## Code Conventions

- **Nullable reference types** enabled across all projects
- **Implicit usings** enabled
- Default to no comments — only add when the *why* is non-obvious
- `Result` / `Result<T>` pattern for operation outcomes (`Application/Common/Result.cs`)
- User roles: `User`, `PowerUser`, `Admin`, `SuperAdmin` (ascending privilege)
- Presentation project has `RootNamespace: App` — controllers use `App.Controllers`, view models use `App.Models`

## Key Files

| Purpose | Path |
| --- | --- |
| Solution | `PV260Project.sln` |
| DI registration | `Infrastructure/DependencyInjection.cs` |
| Result pattern | `Application/Common/Result.cs` |
| Test helpers | `Tests/TestHelpers/SpyLogger.cs`, `Tests/TestHelpers/PostgresFixture.cs` |
| Testing guidelines | `TESTING_GUIDELINES.md` |
| CI pipeline | `.gitlab-ci.yml` |
| Docker Compose | `docker-compose.yml` |