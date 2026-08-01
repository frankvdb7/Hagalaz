using Microsoft.Extensions.Logging;

namespace Hagalaz.Database.Migrations;

public sealed class MigrationApplication
{
    private readonly Func<CancellationToken, Task> _migration;
    private readonly ILogger<MigrationApplication> _logger;

    public MigrationApplication(
        Func<CancellationToken, Task> migration,
        ILogger<MigrationApplication> logger)
    {
        ArgumentNullException.ThrowIfNull(migration);
        ArgumentNullException.ThrowIfNull(logger);
        _migration = migration;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _migration(cancellationToken);
            return 0;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "Database migration failed.");
            return 1;
        }
    }
}
