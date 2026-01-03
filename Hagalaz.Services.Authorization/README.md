# Hagalaz Authorization Service

This service handles user authentication and authorization for the Hagalaz platform.

## Configuration

### Captcha Secrets (Development)

For local development, the Captcha secrets (`SiteKey` and `Secret`) are included in `appsettings.json` as empty strings. These are placeholders and **must** be overridden using the .NET user secrets manager to provide actual values. This prevents sensitive information from being checked into source control.

1.  **Initialize User Secrets:**
    Open a terminal in the `Hagalaz.Services.Authorization` directory and run the following command:
    ```bash
    dotnet user-secrets init
    ```

2.  **Set Captcha Secrets:**
    Use the following commands to set the required captcha secrets, which will override the empty values in `appsettings.json`. Replace the placeholder values with your actual development keys.

    ```bash
    dotnet user-secrets set "Captcha:SiteKey" "your-development-site-key"
    dotnet user-secrets set "Captcha:Secret" "your-development-secret"
    ```

This configuration ensures that the application can access the captcha secrets during development without exposing them in `appsettings.json`. For production environments, these settings should be managed through secure configuration providers like Azure Key Vault or environment variables.
