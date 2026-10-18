using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ShopAPI.Infrastructure;

namespace ShopAPI.IntegrationTests;

// Real PostgreSQL databases, not EF InMemory. TEST_POSTGRES_CONNECTION points to a
// local/CI maintenance database where CREATE/DROP DATABASE is allowed.
public sealed class PgScenario : IAsyncDisposable
{
    private readonly string _adminConnection;
    private readonly string _databaseName;
    private readonly WebApplicationFactory<Program> _factory;
    public HttpClient Client { get; }
    public IServiceProvider Services => _factory.Services;

    private PgScenario(string adminConnection, string databaseName, WebApplicationFactory<Program> factory, HttpClient client)
    {
        _adminConnection = adminConnection; _databaseName = databaseName; _factory = factory; Client = client;
    }

    public static async Task<PgScenario> StartAsync()
    {
        var adminConnection = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION")
            ?? throw new InvalidOperationException("Set TEST_POSTGRES_CONNECTION to run real PostgreSQL integration tests.");
        var name = "shopcore_test_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(adminConnection))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin);
            await create.ExecuteNonQueryAsync();
        }
        var appConn = new NpgsqlConnectionStringBuilder(adminConnection) { Database = name }.ToString();
        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(48));
        var factory = new TestFactory(appConn, key);
        try
        {
            var client = factory.CreateClient();
            using var scope = factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ShopDbContext>().Database.MigrateAsync();
            return new PgScenario(adminConnection, name, factory, client);
        }
        catch
        {
            await factory.DisposeAsync();
            await DropAsync(adminConnection, name);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
        await DropAsync(_adminConnection, _databaseName);
    }

    private static async Task DropAsync(string connection, string name)
    {
        await using var admin = new NpgsqlConnection(connection);
        await admin.OpenAsync();
        await using var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)", admin);
        await cmd.ExecuteNonQueryAsync();
    }

    private sealed class TestFactory(string connection, string key) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connection,
                ["Jwt:Key"] = key,
                ["Jwt:Issuer"] = "ShopCore-Tests",
                ["Jwt:Audience"] = "ShopCore-Test-Clients"
            }));
        }
    }
}
