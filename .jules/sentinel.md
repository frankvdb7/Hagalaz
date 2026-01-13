## 2024-07-22 - Misinterpretation of Placeholders
**Vulnerability:** Initially identified placeholder values in 'appsettings.json' as hardcoded secrets.
**Learning:** Placeholders like 'placeholder' or empty strings in committed configuration files are standard practice for templates. They are not active secrets. The actual security risk is the failure to replace them with real secrets in a secure production environment, not their presence in the repository.
**Prevention:** Before flagging a value in a config file as a secret, I will assess if it's a generic placeholder versus something that looks like a real, leaked credential. I will focus on how secrets are injected at runtime rather than on the template files themselves.
