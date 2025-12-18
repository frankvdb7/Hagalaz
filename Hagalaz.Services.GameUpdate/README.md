# Hagalaz Game Update Service

This service is responsible for providing game client updates.

## 🛡️ Security Configuration

This project uses the .NET Secret Manager to handle sensitive configuration data for local development. Do not store secrets directly in `appsettings.json`.

### Required Secrets

To run this service locally, you must configure the following secrets.

First, navigate to the project directory:
```bash
cd Hagalaz.Services.GameUpdate
```

Then, initialize user secrets (if not already done):
```bash
dotnet user-secrets init
```

Finally, set the following secret values. Replace the placeholder values with your actual development keys.

**Configuration Secrets:**

```bash
dotnet user-secrets set "Configuration:ServerToken" "your-development-server-token"
dotnet user-secrets set "Configuration:UpdateKeys:0" "your-first-update-key"
```
*(Note: Add more `UpdateKeys` as needed by incrementing the index, e.g., `Configuration:UpdateKeys:1`)*

**Cache Keys (RSA keys for cache encryption):**

These must be large integer values.

```bash
dotnet user-secrets set "CacheKeys:PrivateKey" "your-private-key-value"
dotnet user-secrets set "CacheKeys:ModulusKey" "your-modulus-key-value"
dotnet user-secrets set "CacheKeys:PublicKey" "your-public-key-value"
```

After setting these secrets, they will be automatically loaded into the application's configuration during local development.
