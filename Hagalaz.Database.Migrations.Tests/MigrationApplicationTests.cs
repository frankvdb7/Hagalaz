using Hagalaz.Database.Migrations;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hagalaz.Database.Migrations.Tests;

[TestClass]
public sealed class MigrationApplicationTests
{
    [TestMethod]
    public async Task RunAsync_ReturnsSuccess_WhenMigrationCompletes()
    {
        var application = new MigrationApplication(
            _ => Task.CompletedTask,
            NullLogger<MigrationApplication>.Instance);

        var exitCode = await application.RunAsync();

        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    public async Task RunAsync_ReturnsFailure_WhenMigrationThrows()
    {
        var application = new MigrationApplication(
            _ => Task.FromException(new InvalidOperationException("migration failed")),
            NullLogger<MigrationApplication>.Instance);

        var exitCode = await application.RunAsync();

        Assert.AreEqual(1, exitCode);
    }
}
