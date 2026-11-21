using System.Data.Common;
using Testcontainers.MySql;

namespace Hagalaz.Services.Characters.Tests;

/// <summary>
///     One MySQL container shared by every integration test in the assembly. Each test class or test gets its own
///     empty database, so tests stay isolated without paying container startup more than once.
/// </summary>
[TestClass]
public static class SharedMySqlServer
{
    private static readonly Lazy<Task<MySqlContainer>> Container = new(StartAsync);
    private static int _nextDatabaseId;

    /// <summary>
    ///     Creates a new empty database on the shared container and returns its connection string.
    /// </summary>
    public static async Task<string> CreateDatabaseAsync()
    {
        var container = await Container.Value;
        var name = $"characters_test_{Interlocked.Increment(ref _nextDatabaseId)}";
        var result = await container.ExecScriptAsync($"CREATE DATABASE `{name}`;");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Creating database '{name}' failed: {result.Stderr}");
        }

        var builder = new DbConnectionStringBuilder { ConnectionString = container.GetConnectionString() };
        builder["Database"] = name;
        return builder.ConnectionString;
    }

    [AssemblyCleanup]
    public static async Task Cleanup()
    {
        if (Container.IsValueCreated)
        {
            await (await Container.Value).DisposeAsync();
        }
    }

    private static async Task<MySqlContainer> StartAsync()
    {
        var container = new MySqlBuilder("mysql:8.4")
            .WithDatabase("characters-test")
            .WithUsername("root")
            .WithPassword("hagalaz-characters-test")
            .WithCommand(
                "--character-set-server=utf8mb4",
                "--collation-server=utf8mb4_0900_ai_ci")
            .Build();
        await container.StartAsync();
        return container;
    }
}
