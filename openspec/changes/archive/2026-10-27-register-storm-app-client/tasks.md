# Tasks

## 1. Register the browser client

- [x] 1.1 Add an idempotent `storm-app` registration using the existing OpenIddict manager, and test that a missing client is created with only the specified public-client permissions while an existing client is left unchanged. Maps to A1 and A2.

## 2. Verify the live token endpoint

- [x] 2.1 Rebuild the authorization resource through Aspire and submit a token request through the admin proxy with `client_id=storm-app` and synthetic invalid user credentials; verify the response is no longer `invalid_client` and has no `WWW-Authenticate: Basic` header. Maps to A3.

## Workflow follow-up

- Archive the change after acceptance criteria pass and the current behavior specification is synchronized.
