# Testing Guidelines

Conventions and patterns for writing tests in this project.

## Frameworks & Libraries

| Library | Purpose | NuGet Package |
| --- | --- | --- |
| xUnit | Test runner and discovery | `xunit` |
| Shouldly | Assertion library | `Shouldly` |
| Moq | Interface mocking | `Moq` |
| WireMock.Net | HTTP endpoint mocking | `WireMock.Net` |
| Testcontainers | Real PostgreSQL for integration tests | `Testcontainers.PostgreSql` |

## Project Structure

All tests live in a single `Tests/` project. Test directories mirror the source project structure:

```
Tests/
├── Application/
│   ├── Clients/          # Tests for Application/Clients/
│   └── Services/         # Tests for Application/Services/
├── Domain/
│   └── Services/         # Tests for Domain/Services/
├── Infrastructure/
│   ├── CronJobs/         # Tests for Infrastructure/CronJobs/
│   └── Repositories/     # Tests for Infrastructure/Repositories/
├── Presentation/
│   └── Controllers/      # Tests for Presentation/Controllers/
└── TestHelpers/          # Shared utilities (SpyLogger, PostgresFixture)
```

Namespaces follow the directory path: `Tests.Application.Services`, `Tests.Domain.Services`, etc.

## Naming

Test methods: `MethodName_ExpectedBehavior_WhenCondition`

```csharp
Fetch_ReturnsSnapshot_WhenCsvIsValid
DeleteUserAsync_ReturnsFailure_WhenUserIsSuperAdmin
Compute_ReturnsNewEntry_WhenHoldingOnlyInCurrentSnapshot
```

Test files: `{ClassName}Tests.cs` placed in the matching mirror directory.

## Test Structure

Use Arrange-Act-Assert separated by blank lines. No section comments.

```csharp
[Fact]
public async Task FetchLatest_SavesSnapshotOnly_WhenNoPreviousSnapshotExists()
{
    var (service, repoMock, clientMock, _) = CreateService();
    var snapshot = new HoldingSnapshot(DateTimeOffset.UtcNow, []);
    clientMock.Setup(c => c.Fetch()).ReturnsAsync(snapshot);
    repoMock.Setup(r => r.GetLatestSnapshotAsync()).ReturnsAsync((HoldingSnapshot?)null);

    await service.FetchLatest();

    repoMock.Verify(r => r.SaveSnapshotAsync(snapshot), Times.Once);
    repoMock.Verify(r => r.SaveDiffAsync(It.IsAny<HoldingDiff>()), Times.Never);
}
```

## Setup Pattern

Use a factory method returning a tuple with the SUT and its dependencies:

```csharp
private static (HoldingService Service, Mock<IHoldingsRepository> RepoMock,
    Mock<IHoldingClient> ClientMock, SpyLogger<HoldingService> Logger) CreateService(
    int deleteAfterDays = 14)
{
    var repoMock = new Mock<IHoldingsRepository>();
    var clientMock = new Mock<IHoldingClient>();
    var logger = new SpyLogger<HoldingService>();
    var options = Options.Create(new DataRetentionSettings { DeleteAfterDays = deleteAfterDays });
    var service = new HoldingService(repoMock.Object, clientMock.Object, options, logger);
    return (service, repoMock, clientMock, logger);
}
```

## Assertions

Use Shouldly exclusively.

```csharp
result.ShouldNotBeNull();
result.ShouldBe(expected);
result.IsSuccess.ShouldBeTrue();
result.Error.ShouldBe("User not found.");
list.ShouldBeEmpty();
list.Count.ShouldBe(2);
entry.Type.ShouldBe(DiffType.New);
```

For logging verification, use `SpyLogger<T>` (in `Tests/TestHelpers/`):

```csharp
logger.HasErrorOrAbove.ShouldBeTrue();
logger.HasWarningOrAbove.ShouldBeFalse();
```

## Test Attributes

Both `[Fact]` and `[Theory]`/`[InlineData]` are acceptable.

```csharp
[Theory]
[InlineData(UserRole.User)]
[InlineData(UserRole.PowerUser)]
public async Task UpdateRoleAsync_Succeeds_WhenAdminAssignsManageableRole(UserRole newRole)
{
    // ...
}
```

## Test Data

Define test data as `private const` or `private static readonly` at the top of the class.

```csharp
private const string ValidCsvContent =
    "date,fund,company,ticker,cusip,shares,market value($),weight(%)\n" +
    "04/25/2025,ARKK,Tesla Inc,TSLA,88160R101,\"3,000,000\",\"$750,000,000\",\"10.50%\"";

private static readonly HoldingRecord TestRecord = new("Tesla Inc", "TSLA", "88160R101", 3_000_000, 750_000_000m, 10.50m);
```

## Resource Cleanup

- `IDisposable` for synchronous cleanup (e.g., WireMock server)
- `IAsyncLifetime` for asynchronous cleanup (e.g., Testcontainers)

## Global Usings

`Xunit` is globally imported via `Tests.csproj`. Do not add `using Xunit;` in test files.

## Layer-Specific Test Strategies

### Domain (pure unit tests)

No mocking needed. Test constructors, computed properties, static methods, and edge cases.

```csharp
public class HoldingDiffCalculatorTests
{
    [Fact]
    public void Compute_ReturnsRemovedEntry_WhenHoldingOnlyInPreviousSnapshot()
    {
        var previous = new HoldingSnapshot(DateTimeOffset.UtcNow, [TestRecord]);
        var current = new HoldingSnapshot(DateTimeOffset.UtcNow, []);

        var diff = HoldingDiffCalculator.Compute(previous, current);

        diff.Entries.Count.ShouldBe(1);
        diff.Entries[0].Type.ShouldBe(DiffType.Removed);
    }
}
```

### Application Services (unit tests with Moq)

Mock all constructor dependencies. Use `SpyLogger<T>` when verifying log output. Verify delegation with `Mock.Verify()`. Assert `Result`/`Result<T>` outcomes.

### Application Clients (integration tests with WireMock)

Use `WireMockServer` to simulate HTTP endpoints. Test success paths, error codes, retries, timeouts.

### Infrastructure Repositories (integration tests with Testcontainers)

Use `PostgresFixture` via `IClassFixture<PostgresFixture>` to get a real PostgreSQL container. Configure Marten with the container's connection string.

```csharp
public class HoldingsRepositoryTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public HoldingsRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    // Tests run against a real PostgreSQL instance
}
```

### Controllers (unit tests with Moq)

Mock service interfaces. Test action results, redirects, and view model construction.
