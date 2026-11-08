## ADDED Requirements

### Requirement: Cache API introspection client is registered

The authorization service SHALL register the Cache API as a confidential client that can authenticate to the introspection endpoint using the configured shared secret.

#### Scenario: Cache API introspection client is missing
- **WHEN** the authorization service starts and no `cache-service-1` registration exists
- **THEN** it creates a confidential client with the configured `Security:Introspection:ClientSecret`
- **AND** grants that client the introspection endpoint permission
- **AND** does not grant it token, password-grant, or refresh-token permissions

#### Scenario: Cache API introspection secret changes
- **WHEN** an existing `cache-service-1` registration does not validate against the configured `Security:Introspection:ClientSecret`
- **THEN** the authorization service updates the stored client secret
- **AND** preserves the registration's other application settings and permissions

#### Scenario: Cache API validates a bearer token
- **WHEN** Cache API sends a bearer token to the configured Authorization introspection endpoint using `client_id=cache-service-1` and the matching configured client secret
- **THEN** Authorization authenticates the registered Cache API client and evaluates the token
