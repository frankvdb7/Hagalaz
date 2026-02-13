## 2024-07-22 - Misinterpretation of Placeholders
**Vulnerability:** Initially identified placeholder values in 'appsettings.json' as hardcoded secrets.
**Learning:** Placeholders like 'placeholder' or empty strings in committed configuration files are standard practice for templates. They are not active secrets. The actual security risk is the failure to replace them with real secrets in a secure production environment, not their presence in the repository.
**Prevention:** Before flagging a value in a config file as a secret, I will assess if it's a generic placeholder versus something that looks like a real, leaked credential. I will focus on how secrets are injected at runtime rather than on the template files themselves.

## 2025-05-24 - Secure Password Migration Pattern
**Vulnerability:** Legacy use of SHA256(Email + password) and support for plaintext passwords in the database.
**Learning:** When upgrading a legacy authentication system to a modern standard like PBKDF2, a direct replacement is not possible without breaking existing user accounts. The solution is to use `PasswordVerificationResult.SuccessRehashNeeded` in a custom `IPasswordHasher`.
**Prevention:** Implement a multi-layered verification logic: first try the modern standard, then fall back to legacy methods. If a legacy method succeeds, return `SuccessRehashNeeded`. This leverages ASP.NET Core Identity's built-in mechanism to automatically upgrade the hash on the next successful login.
