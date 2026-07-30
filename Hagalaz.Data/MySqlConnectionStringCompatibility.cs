using System;
using System.Data.Common;

namespace Hagalaz.Data;

/// <summary>
/// Normalizes connection-string values written by the legacy MySqlConnector
/// provider before they are parsed by Oracle Connector/NET.
/// </summary>
public static class MySqlConnectionStringCompatibility
{
    public static string NormalizeForOracle(string connectionString, string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        try
        {
            var connectionStringBuilder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            foreach (var keyValue in connectionStringBuilder.Keys)
            {
                if (keyValue is not string key)
                {
                    continue;
                }

                if (!IsSslModeKey(key) ||
                    !string.Equals(Convert.ToString(connectionStringBuilder[key]), "None", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // MySqlConnector serializes this option as "SSL Mode=None" while
                // Connector/NET uses Disabled for an unencrypted connection.
                connectionStringBuilder[key] = "Disabled";
                return connectionStringBuilder.ConnectionString;
            }

            return connectionString;
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"The {source} MySQL connection string is invalid. " +
                "Verify its key/value syntax before starting the service.",
                exception);
        }
    }

    private static bool IsSslModeKey(string key) =>
        string.Equals(key.Replace(" ", string.Empty, StringComparison.Ordinal), "SslMode", StringComparison.OrdinalIgnoreCase);
}
