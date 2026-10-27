# Tasks

## 1. Separate the frontend client IDs

- [x] 1.1 Keep the normal auth service on `storm-app`, change the admin auth service to `storm-admin`, and test each request's `client_id` and scopes with the corresponding Angular test project. Maps to A1 and A2.

## 2. Register clients with separate permissions

- [x] 2.1 Ensure missing `storm-app` and `storm-admin` registrations are created with their exact public-web permission sets; remove only `cache_api` from an existing `storm-app` descriptor while preserving other settings; test both creation and reconciliation. Maps to A3 and A4.

## 3. Verify the live authorization flow

- [x] 3.1 Rebuild the authorization resource through Aspire, wait for healthy state, and submit synthetic invalid-credential token requests through the admin proxy for both client IDs; verify neither returns `invalid_client` or `WWW-Authenticate: Basic`. Maps to A5.

## Workflow follow-up

- Sync the modified capability spec and archive the change after acceptance criteria pass.
