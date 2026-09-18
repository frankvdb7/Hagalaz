Use the existing `OpenIddictServerConfiguration` owner for issuer and
credential setup. `Program` invokes the issuer configuration before credential
configuration. Every environment requires an explicit `OpenIddict:Issuer`;
there is no launch-profile port fallback. The Aspire AppHost supplies the
development value from the authorization service's named HTTPS endpoint.
Development may explicitly use HTTP, while non-development configuration fails
fast unless the issuer is HTTPS, absolute, and has no query or fragment. No
second token or authentication path is introduced.
