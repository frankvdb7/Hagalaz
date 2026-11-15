# Spec Delta

## Purpose

Defines the authorization service's registration of the first-party browser client used by the Hagalaz web applications.

## ADDED Requirements

### Requirement: First-party web client is registered when missing

The authorization service SHALL ensure the `storm-app` browser client is registered so the existing web applications can use their password and refresh-token login flow.

#### Scenario: Client registration is missing
- **WHEN** the authorization service starts and no `storm-app` registration exists
- **THEN** it creates a public web client registration with the token and end-session endpoints, password and refresh-token grants, and the scopes requested by the web applications
- **AND** it does not configure a client secret

#### Scenario: Client registration already exists
- **WHEN** the authorization service starts and a `storm-app` registration already exists
- **THEN** it leaves that registration unchanged

#### Scenario: Browser submits a password grant
- **WHEN** a web application submits a password-grant request using `client_id=storm-app`
- **THEN** the authorization service evaluates the grant and user credentials instead of rejecting the request as an unknown client
- **AND** it does not issue an HTTP Basic challenge for that registered client
