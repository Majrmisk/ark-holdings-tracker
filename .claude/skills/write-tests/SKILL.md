---
name: write-tests
description: >-
  Generate and validate xUnit tests for .NET classes in this Clean Architecture project.
  Use when the user asks to write tests, create tests, add tests, generate test cases,
  validate existing tests, check test coverage, or test a specific class/service/controller.
  Supports targeting by class name argument or by selected code in the editor.
---

# Write Tests

Generate and validate xUnit tests for this .NET 10 Clean Architecture project.

## Step 0: Read the conventions

Read `TESTING_GUIDELINES.md` at the project root. All generated tests must follow
these conventions exactly.

## Step 1: Determine the target

**If an argument is provided** (e.g., `/write-tests UserService`):
- Treat it as a class name or file path.
- Find the source file:
  ```bash
  find . -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" | xargs grep -l "class <argument>"
  ```
- Read the full source file.

**If selected code is provided** (IDE selection):
- Use the selected code directly.
- Identify the class it belongs to.

**If neither is provided** (`/write-tests` with no argument):
- Scan all source files across Domain, Application, Infrastructure, Presentation.
- Scan all existing test files in `Tests/`.
- Identify which classes have tests and which don't.
- Validate existing tests cover the right things (correct method coverage, edge cases).
- Report a coverage summary, then generate tests for uncovered classes.
- Build and run all tests when done.

## Step 2: Identify the architectural layer

Determine which layer the target class belongs to by its namespace or project:

| Namespace pattern | Layer | Test strategy |
| --- | --- | --- |
| `Domain` or `Domain.*` | Domain | Pure unit tests, no mocking |
| `Application.Services` | Application Service | Unit tests with Moq for interfaces |
| `Application.Clients` | Application Client | Integration tests with WireMock.Net |
| `Infrastructure.Repositories` | Infrastructure Repo | Integration tests with Testcontainers |
| `Infrastructure.CronJobs` | Infrastructure Job | Integration tests with Testcontainers + Moq |
| `*.Controllers` or `Presentation.*` | Presentation | Unit tests with Moq for services |

## Step 3: Read existing tests

1. Read ALL existing test files in `Tests/` to understand current patterns:
   ```bash
   find Tests/ -name "*Tests.cs" -not -path "*/obj/*" -not -path "*/bin/*"
   ```
2. Read each file to understand the established style.
3. Check if tests already exist for the target class.
   - If yes: validate coverage (are all public methods tested? are edge cases covered?)
     and extend with missing test cases.
   - If no: generate a complete test file from scratch.

## Step 4: Read dependencies

1. Read all interfaces the target class implements or depends on (constructor parameters).
2. Read Domain types referenced by the class (entities, enums, value objects).
3. Read the `Result` and `Result<T>` types in `Application/Common/Result.cs` if the
   target returns them.

## Step 5: Ensure project dependencies

Check `Tests/Tests.csproj` for required packages and references.

**Packages** — add if not present:

```xml
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="Shouldly" Version="4.3.0" />
<PackageReference Include="Testcontainers.PostgreSql" Version="4.11.0" />
```

**Project references** — add only when needed:
- Testing Domain or Application classes: no extra reference needed (Application is already
  referenced, and it transitively includes Domain).
- Testing Infrastructure classes: add `<ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />`
- Testing Presentation controllers with Infrastructure types: add both Infrastructure
  and Presentation project references.

Warn the user about any additions and explain why.

## Step 6: Generate the test file

### File location

Place the test file at `Tests/{MirrorPath}/{ClassName}Tests.cs` where `{MirrorPath}` is the
full relative path from the solution root to the source file's directory, preserving all
nested folders. For example, a source file at `Presentation/Areas/Admin/Controllers/UsersController.cs`
maps to `Tests/Presentation/Areas/Admin/Controllers/UsersControllerTests.cs` — do not
flatten or skip intermediate directories:

| Source path | Test path |
| --- | --- |
| `Domain/Services/HoldingDiffCalculator.cs` | `Tests/Domain/Services/HoldingDiffCalculatorTests.cs` |
| `Application/Services/UserService.cs` | `Tests/Application/Services/UserServiceTests.cs` |
| `Application/Clients/HoldingClient.cs` | `Tests/Application/Clients/HoldingClientTests.cs` |
| `Infrastructure/Repositories/HoldingsRepository.cs` | `Tests/Infrastructure/Repositories/HoldingsRepositoryTests.cs` |
| `Presentation/Controllers/TableController.cs` | `Tests/Presentation/Controllers/TableControllerTests.cs` |
| `Presentation/Areas/Admin/Controllers/UsersController.cs` | `Tests/Presentation/Areas/Admin/Controllers/UsersControllerTests.cs` |

### Namespace

Use `Tests.{MirrorPath}` matching the full directory structure, including nested paths:
`Tests.Domain.Services`, `Tests.Application.Services`, `Tests.Presentation.Areas.Admin.Controllers`, etc.

### Test class structure

```csharp
using <necessary usings — but NOT Xunit, it is globally imported>;

namespace Tests.<MirrorPath>;

public class <ClassName>Tests : IDisposable  // IDisposable only if cleanup needed
{
    // Constants for test data at the top
    private const string SomeConstant = "value";

    // Factory method returning tuple with SUT and mocks
    private static (<ReturnTypes>) Create<ClassName>(<optional params>)
    {
        // Shared setup
        return (<instances>);
    }

    [Fact]
    public async Task MethodName_ExpectedBehavior_WhenCondition()
    {
        var (sut, mockA, mockB) = Create<ClassName>();
        mockA.Setup(x => x.SomeMethod()).ReturnsAsync(someValue);

        var result = await sut.MethodUnderTest();

        result.ShouldNotBeNull();
        mockA.Verify(x => x.SomeMethod(), Times.Once);
    }

    public void Dispose() => /* cleanup */;
}
```

### Integration tests with Testcontainers

For Infrastructure classes, use `IClassFixture<PostgresFixture>`:

```csharp
using Tests.TestHelpers;

namespace Tests.Infrastructure.Repositories;

public class HoldingsRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public HoldingsRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SaveSnapshotAsync_PersistsSnapshot_WhenValid()
    {
        using var store = _fixture.CreateDocumentStore();
        await using var session = store.LightweightSession();
        var repo = new HoldingsRepository(session);
        var snapshot = new HoldingSnapshot(DateTimeOffset.UtcNow, []);

        await repo.SaveSnapshotAsync(snapshot);

        var retrieved = await session.Query<HoldingSnapshot>().FirstOrDefaultAsync();
        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(snapshot.Id);
    }
}
```

### Minimum coverage per public method

For each public method, generate at minimum:
1. **Happy path**: normal successful execution.
2. **Null/empty input**: what happens with null or missing data.
3. **Error path**: each distinct failure mode.
4. **Boundary conditions**: edge cases specific to the method's logic.
5. **Delegation verification** (for services): verify correct underlying methods are called
   with `Mock.Verify()`.

### SpyLogger usage

`SpyLogger<T>` is defined in `Tests/TestHelpers/SpyLogger.cs`. Use it when tests need
to verify logging behavior:

```csharp
using Tests.TestHelpers;

var logger = new SpyLogger<HoldingService>();
// ... run code that logs ...
logger.HasWarningOrAbove.ShouldBeTrue();
```

## Step 7: Build and run

After generating the test file:

1. Build the test project:
   ```bash
   dotnet build Tests/Tests.csproj
   ```
2. Run all tests:
   ```bash
   dotnet test Tests/Tests.csproj
   ```
3. If any test fails, diagnose and fix the issue before reporting the task as complete.
