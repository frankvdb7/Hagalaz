## 2024-07-22 - Misinterpretation of Placeholders
**Vulnerability:** Initially identified placeholder values in 'appsettings.json' as hardcoded secrets.
**Learning:** Placeholders like 'placeholder' or empty strings in committed configuration files are standard practice for templates. They are not active secrets. The actual security risk is the failure to replace them with real secrets in a secure production environment, not their presence in the repository.
**Prevention:** Before flagging a value in a config file as a secret, I will assess if it's a generic placeholder versus something that looks like a real, leaked credential. I will focus on how secrets are injected at runtime rather than on the template files themselves.

## 2025-05-15 - Secure Password Hashing Migration
**Vulnerability:** Weak SHA256(Email + password) hashing without random salt or iterations, and support for plaintext passwords.
**Learning:** Legacy systems often use weak hashing or plaintext. Migrating these requires a strategy that doesn't disrupt users. Using `PasswordVerificationResult.SuccessRehashNeeded` in ASP.NET Core Identity allows for seamless, transparent upgrades of user credentials upon their next successful login.
**Prevention:** Always use standard, proven hashing libraries (like ASP.NET Core Identity's `PasswordHasher`) instead of custom implementations. When migrating, implement a versioning or prefix-based detection (e.g., checking for `AQAAAA`) to distinguish between hash versions.
