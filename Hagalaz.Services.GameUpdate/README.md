# Hagalaz.Services.GameUpdate

This service is responsible for handling game client updates. It provides the necessary data for the client to ensure it is up to date with the latest version of the game.

## Configuration

This service requires specific configuration values to be set for local development. These secrets should not be stored in `appsettings.json` and must be configured using the .NET user secrets manager.

### Secret Management

To configure the necessary secrets for local development, navigate to this project's directory in your terminal and run the following commands. Replace the placeholder values with the appropriate secrets for your development environment.

```bash
# Set the server token used to validate client requests
dotnet user-secrets set "Configuration:ServerToken" "your-development-server-token"

# Set the update keys required for the client update process
dotnet user-secrets set "Configuration:UpdateKeys:0" "your-development-update-key"

# Set the private and modulus keys for cache encryption
dotnet user-secrets set "CacheKeys:PrivateKey" "your-development-private-key"
dotnet user-secrets set "CacheKeys:ModulusKey" "your-development-modulus-key"
```

This configuration ensures that the application can access these secrets during development without exposing them in `appsettings.json`. For production environments, these settings should be managed through secure configuration providers like Azure Key Vault or environment variables.
