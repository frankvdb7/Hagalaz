# Authorization Client Registration Specification

## Purpose

Defines the authorization service's registration of the first-party browser client used by the Hagalaz web applications.

## Requirements

### Requirement: First-party web client is registered when missing

The authorization service SHALL register separate public web clients for the normal and admin frontends so each can use its password and refresh-token login flow with only its approved scopes.

#### Scenario: Normal client registration is missing
- **WHEN** the authorization service starts and no `storm-app` registration exists
- **THEN** it creates a public web client registration with the token and end-session endpoints, password and refresh-token grants, and the `email`, `profile`, `roles`, and `characters_api` scopes
- **AND** it does not configure a client secret or grant the `cache_api` scope

#### Scenario: Admin client registration is missing
- **WHEN** the authorization service starts and no `storm-admin` registration exists
- **THEN** it creates a public web client registration with the token and end-session endpoints, password and refresh-token grants, and the `email`, `profile`, `roles`, `characters_api`, and `cache_api` scopes
- **AND** it does not configure a client secret

#### Scenario: Existing normal client has the admin scope
- **WHEN** a `storm-app` registration already exists with the `cache_api` permission
- **THEN** the authorization service removes only the `cache_api` permission
- **AND** it preserves the registration's other settings

#### Scenario: Admin client registration already exists
- **WHEN** a `storm-admin` registration already exists
- **THEN** the authorization service leaves that registration unchanged

#### Scenario: Frontends submit password grants
- **WHEN** the normal frontend submits a password grant with `client_id=storm-app` or the admin frontend submits one with `client_id=storm-admin`
- **THEN** the authorization service evaluates the grant and user credentials instead of rejecting either request as an unknown client
- **AND** it does not issue an HTTP Basic challenge for either registered client
