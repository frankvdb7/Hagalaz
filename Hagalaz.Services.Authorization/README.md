# Hagalaz Authorization Service

This service handles user authentication and authorization for the Hagalaz platform.

## Configuration

### Captcha Secrets (Development)

For local development, the Captcha secrets (`SiteKey` and `Secret`) must be configured using the .NET user secrets manager. This prevents sensitive information from being checked into source control. The non-secret `ApiBaseUrl` is already configured in `appsettings.json`.

1.  **Initialize User Secrets:**
    Open a terminal in the `Hagalaz.Services.Authorization` directory and run the following command:
    ```bash
    dotnet user-secrets init
    ```

2.  **Set Captcha Secrets:**
    Use the following commands to set the required captcha secrets. Replace the placeholder values with your actual development keys.

    ```bash
    dotnet user-secrets set "Captcha:SiteKey" "your-development-site-key"
    dotnet user-secrets set "Captcha:Secret" "your-development-secret"
    ```

This configuration ensures that the application can access the captcha secrets during development without exposing them in `appsettings.json`. For production environments, these settings should be managed through secure configuration providers like Azure Key Vault or environment variables.
