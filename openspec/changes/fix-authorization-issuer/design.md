Use the existing `OpenIddictServerConfiguration` owner for issuer and
credential setup. `Program` invokes the issuer configuration before credential
configuration. An explicit `OpenIddict:Issuer` value is authoritative; only
Development may derive a localhost URI from the launch-profile HTTPS/HTTP port.
Non-development configuration fails fast unless the issuer is absolute, has no
query or fragment, and uses HTTPS. No second token or authentication path is
introduced.
