# Hagalaz Authorization Service

This service is responsible for handling user authentication and authorization within the Hagalaz ecosystem. It manages user sign-in, token generation, and validation.

## Configuration

### hCaptcha Secret

For local development, the hCaptcha secret is managed using the .NET User Secrets mechanism. To configure the secret, follow these steps:

1.  **Initialize User Secrets**:
    Open a terminal in this project's directory (`Hagalaz.Services.Authorization`) and run the following command to initialize user secrets:
    ```bash
    dotnet user-secrets init
    ```

2.  **Set the Secret**:
    Use the following command to set the hCaptcha secret. Replace `"YourSecretHere"` with the actual secret provided by hCaptcha.
    ```bash
    dotnet user-secrets set "Captcha:Secret" "YourSecretHere"
    ```

This ensures that the secret is stored securely on your local machine and is not checked into source control. For production environments, it is recommended to use a secure configuration provider like Azure Key Vault or HashiCorp Vault.
