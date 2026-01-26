# Hagalaz.Services.GameWorld

This service is the core of the game world, handling game logic, player state, and interactions.

## Local Development Setup

For security reasons, sensitive configuration values are **not** stored in `appsettings.json`. To run this service locally, you must configure the required secrets using the .NET User Secrets manager.

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download)

### Configuration Steps

1.  **Navigate to the service directory:**
    ```bash
    cd Hagalaz.Services.GameWorld
    ```

2.  **Initialize User Secrets for the project:**
    ```bash
    dotnet user-secrets init
    ```

3.  **Set the required secrets:**
    The following secrets are required to run the application. Replace `"..."` with the appropriate values for your development environment.

    ```bash
    dotnet user-secrets set "GameServer:AuthenticationToken" "your-development-token"
    dotnet user-secrets set "CacheKeys:PrivateKey" "your-cache-private-key"
    dotnet user-secrets set "CacheKeys:ModulusKey" "your-cache-modulus-key"
    dotnet user-secrets set "ClientKeys:PrivateKey" "your-client-private-key"
    dotnet user-secrets set "ClientKeys:ModulusKey" "your-client-modulus-key"
    ```

    *   `GameServer:AuthenticationToken`: A token for server-to-server authentication.
    *   `CacheKeys`: Cryptographic keys used for cache-related operations.
    *   `ClientKeys`: Cryptographic keys used for client-related operations.

After setting these secrets, you can run the service locally, and it will automatically load these values from your user secrets file. Your secrets are stored securely in a JSON file in your user profile directory, separate from the project source code.
