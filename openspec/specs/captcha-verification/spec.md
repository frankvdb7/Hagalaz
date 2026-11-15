# captcha-verification Specification

## Purpose

Defines how the authorization service verifies CAPTCHA tokens submitted by the Hagalaz admin application before credential sign-in.

## Requirements

### Requirement: Admin CAPTCHA verification returns provider outcome

The authorization service SHALL accept the admin application's CAPTCHA verification request and return whether the configured CAPTCHA provider accepted the submitted token.

#### Scenario: Provider accepts a token
- **WHEN** the admin application submits a valid CAPTCHA token
- **THEN** the verification response reports success

#### Scenario: Provider rejects a token
- **WHEN** the admin application submits a token rejected by the configured provider
- **THEN** the verification response reports failure

#### Scenario: Missing verification request
- **WHEN** the admin application submits an empty CAPTCHA verification request
- **THEN** the authorization service returns a client error without contacting the provider
