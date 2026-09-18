## ADDED Requirements

### Requirement: Background token creation has a stable issuer

The authorization service MUST configure a valid absolute OpenIddict issuer so
token creation from a MassTransit consumer does not depend on an HTTP request.

#### Scenario: Missing development issuer

- **WHEN** Development configuration has no explicit `OpenIddict:Issuer`, regardless of launch-profile port variables
- **THEN** configuration fails with an error naming `OpenIddict:Issuer`

#### Scenario: Explicit HTTP Development issuer

- **WHEN** Development configuration explicitly sets an HTTP `OpenIddict:Issuer`
- **THEN** that URI is configured as the OpenIddict issuer

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

#### Scenario: Aspire injects development issuer

- **WHEN** the AppHost configures the authorization service
- **THEN** it exposes a named HTTPS endpoint and sets `OpenIddict__Issuer` from that endpoint reference
