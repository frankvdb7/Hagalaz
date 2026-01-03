# Hagalaz.Services.GameWorld

This service is responsible for managing the core game world logic.

## 🛡️ Local Development & Security

For security, sensitive configuration values like authentication tokens and private keys are **not** stored in `appsettings.json`. This project uses [.NET User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets) to manage sensitive data during local development.

### Required Secrets

To configure this service for local development, navigate to this directory (`Hagalaz.Services.GameWorld/`) and run the following commands, replacing the placeholder values with your local development secrets:

```bash
# Set the authentication token for the game server
dotnet user-secrets set "GameServer:AuthenticationToken" "your_local_token_here"

# Set the RSA keys for the cache
dotnet user-secrets set "CacheKeys:PrivateKey" "your_local_cache_private_key"
dotnet user-secrets set "CacheKeys:ModulusKey" "your_local_cache_modulus_key"

# Set the RSA keys for the client
dotnet user-secrets set "ClientKeys:PrivateKey" "your_local_client_private_key"
dotnet user-secrets set "ClientKeys:ModulusKey" "your_local_client_modulus_key"
```

**Note:** In production environments, these values should be managed by a secure configuration provider like Azure Key Vault or environment variables.
