# Tasks

## 1. Restore the CAPTCHA verification request

- [x] 1.1 Remove the unsupported antiforgery requirement from the JSON action, represent the hCaptcha call as a URL-encoded Refit body, add the matching Refit runtime-builder package, and allow provider responses without `hostname`; verify the authorization resource rebuilds. Maps to A1, A2, and A3.

## 2. Verify provider outcomes through Aspire

- [x] 2.1 Send hCaptcha's published success and rejection test tokens through the admin proxy; verify the endpoint returns the corresponding success and failure results. Maps to A1 and A2.

## Workflow follow-up

- Archive the change after the acceptance criteria pass and the current behavior specification is synchronized.
