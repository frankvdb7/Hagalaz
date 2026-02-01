## 2024-07-22 - Misinterpretation of Placeholders
**Vulnerability:** Initially identified placeholder values in 'appsettings.json' as hardcoded secrets.
**Learning:** Placeholders like 'placeholder' or empty strings in committed configuration files are standard practice for templates. They are not active secrets. The actual security risk is the failure to replace them with real secrets in a secure production environment, not their presence in the repository.
**Prevention:** Before flagging a value in a config file as a secret, I will assess if it's a generic placeholder versus something that looks like a real, leaked credential. I will focus on how secrets are injected at runtime rather than on the template files themselves.

## 2025-01-24 - Regression of Password Hashing Security
**Vulnerability:** The `HagalazPasswordHasher` had regressed to (or was never updated from) a weak SHA256 implementation that used `email + password` without a proper salt and, crucially, allowed plaintext password comparisons if the database stored them.
**Learning:** Even if memory suggests a feature has been refactored, the code is the source of truth. A critical security component like a password hasher should always be verified against its expected secure implementation. Migrating from weak hashes to modern standards (like PBKDF2) requires a clear identification strategy (e.g., checking for the 'AQAAAA' prefix) and returning `SuccessRehashNeeded` to allow automatic migration.
**Prevention:** Always verify security-critical logic against best practices (e.g., ASP.NET Core Identity defaults) and ensure that legacy support does not inadvertently maintain vulnerabilities like plaintext comparisons.
