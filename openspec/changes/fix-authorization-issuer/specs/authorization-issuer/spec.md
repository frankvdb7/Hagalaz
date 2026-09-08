## ADDED Requirements

### Requirement: Background token creation has a stable issuer

The authorization service MUST configure a valid absolute OpenIddict issuer so
token creation from a MassTransit consumer does not depend on an HTTP request.

#### Scenario: Aspire Development issuer

- **WHEN** Development configuration has no explicit issuer and exposes an HTTPS launch-profile port
- **THEN** the authorization service uses the localhost HTTPS URI for that port

#### Scenario: HTTP-only Development issuer

- **WHEN** Development configuration has no explicit issuer, no valid HTTPS launch-profile port, and exposes an HTTP launch-profile port
- **THEN** the authorization service uses the localhost HTTP URI for that port

#### Scenario: Explicit issuer

- **WHEN** `OpenIddict:Issuer` is configured with an absolute URI
- **THEN** that URI is configured as the OpenIddict issuer

### Requirement: Production issuer configuration fails closed

Non-development authorization service configuration MUST reject a missing,
relative, query-bearing, fragment-bearing, or non-HTTPS issuer during startup.

#### Scenario: Missing production issuer

- **WHEN** a non-development service starts without `OpenIddict:Issuer`
- **THEN** configuration fails with an error naming `OpenIddict:Issuer`

#### Scenario: Non-HTTPS production issuer

- **WHEN** a non-development service configures an HTTP issuer
- **THEN** configuration fails before token processing
