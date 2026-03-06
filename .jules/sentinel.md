## 2024-07-22 - Misinterpretation of Placeholders
**Vulnerability:** Initially identified placeholder values in 'appsettings.json' as hardcoded secrets.
**Learning:** Placeholders like 'placeholder' or empty strings in committed configuration files are standard practice for templates. They are not active secrets. The actual security risk is the failure to replace them with real secrets in a secure production environment, not their presence in the repository.
**Prevention:** Before flagging a value in a config file as a secret, I will assess if it's a generic placeholder versus something that looks like a real, leaked credential. I will focus on how secrets are injected at runtime rather than on the template files themselves.

## 2026-03-06 - Custom Weak Hashing and Lack of Password Policy
**Vulnerability:** The application used a custom SHA256(Email + password) hashing scheme and lacked enforced password complexity requirements.
**Learning:** Legacy systems often carry over insecure authentication practices. Using a standard, well-tested framework like ASP.NET Core Identity's PasswordHasher provides a secure-by-default implementation (PBKDF2) and a built-in mechanism (SuccessRehashNeeded) for transparent migration.
**Prevention:** Avoid custom hashing logic. Always leverage standard security libraries and enforce strong password policies (length, character variety) in configuration.
