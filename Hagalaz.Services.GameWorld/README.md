# Hagalaz.Services.GameWorld

This service handles core game world logic and requires sensitive configuration values for local development.

## Local Development Secrets

For security reasons, sensitive configuration values are not stored in `appsettings.json`. Instead, this project uses the .NET User Secrets mechanism to store them locally on your development machine.

### Setup Instructions

To set up the necessary secrets for this service, run the following commands from the `Hagalaz.Services.GameWorld` directory:

```bash
# Initialize user secrets for this project (only needs to be done once)
dotnet user-secrets init

# Set the required secrets
dotnet user-secrets set "GameServer:AuthenticationToken" "your-secure-token-here"
dotnet user-secrets set "CacheKeys:PrivateKey" "your-cache-private-key"
dotnet user-secrets set "CacheKeys:ModulusKey" "your-cache-modulus-key"
dotnet user-secrets set "ClientKeys:PrivateKey" "your-client-private-key"
dotnet user-secrets set "ClientKeys:ModulusKey" "your-client-modulus-key"
```

**Note:** Replace the placeholder values (`"your-secure-token-here"`, etc.) with the actual secret values required for your development environment. These values will be stored securely on your machine and will not be checked into source control.
