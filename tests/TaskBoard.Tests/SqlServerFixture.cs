using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace TaskBoard.Tests;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly string databaseName = $"TaskBoardTests_{Guid.NewGuid():N}";
    private string masterConnection = string.Empty;
    private string testConnection = string.Empty;
    private bool databaseCreated;
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Consume normal configuration without enumerating or logging secrets.
        var configuration = new ConfigurationBuilder().AddUserSecrets<Program>()
            .AddEnvironmentVariables().Build();
        var connection = new SqlConnectionStringBuilder(configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Configure DefaultConnection for SQL Server integration tests."));
        connection.InitialCatalog = "master";
        masterConnection = connection.ConnectionString;
        connection.InitialCatalog = databaseName;
        testConnection = connection.ConnectionString;

        // Only substitute the database name; canonical table definitions stay unchanged.
        var schema = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Database", "001_create.sql"));
        schema = schema.Replace("TaskBoard", databaseName, StringComparison.Ordinal);
        await using var sql = new SqlConnection(masterConnection);
        await sql.OpenAsync();
        foreach (var batch in Regex.Split(schema, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            await using var command = new SqlCommand(batch, sql);
            await command.ExecuteNonQueryAsync();
            databaseCreated = true;
        }
        Factory = new TestApiFactory(testConnection);
    }

    public async Task<int> UserCountAsync()
    {
        await using var sql = new SqlConnection(testConnection);
        await sql.OpenAsync();
        await using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.Users", sql);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    public async Task<(string Name, string Email)> ReadUserAsync(int id)
    {
        await using var sql = new SqlConnection(testConnection);
        await sql.OpenAsync();
        await using var command = new SqlCommand("SELECT Name, Email FROM dbo.Users WHERE Id = @id", sql);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.GetString(0), reader.GetString(1));
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        if (!databaseCreated) return;
        // This identifier is generated locally, never supplied by configuration.
        if (!Regex.IsMatch(databaseName, @"^TaskBoardTests_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Refusing to clean up an unexpected database name.");
        SqlConnection.ClearAllPools();
        await using var sql = new SqlConnection(masterConnection);
        await sql.OpenAsync();
        await using var command = new SqlCommand($"IF DB_ID(N'{databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]; END", sql);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class TestApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connectionString }));
        }
    }
}
