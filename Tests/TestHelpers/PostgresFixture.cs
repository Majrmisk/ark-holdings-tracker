using Marten;
using Testcontainers.PostgreSql;

namespace Tests.TestHelpers;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public IDocumentStore CreateDocumentStore()
    {
        return DocumentStore.For(opts =>
        {
            opts.Connection(ConnectionString);
        });
    }

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
