# Proposal

## Why

Admin sign-in currently fails while verifying its CAPTCHA token, before the authorization service evaluates credentials. The admin's JSON verification request is rejected at the endpoint, and the downstream hCaptcha client cannot build its request with the current Refit contract.

## What Changes

- Accept the admin app's CAPTCHA verification request and return the configured provider's success or failure result.
- Send hCaptcha verification fields using its supported request format through the existing Refit client.
- Include Refit's opt-in runtime request builder because the live authorization client currently falls back to it.

## Capabilities

### New Capabilities
- `captcha-verification`: Authorization service behavior for validating CAPTCHA tokens submitted by the admin app.

### Modified Capabilities
None.

## Impact

Authorization service controller, hCaptcha client contract, and provider response model. Reuse the existing controller, `ICaptchaService`, Refit registration, and configured hCaptcha options; add the matching optional Refit runtime-builder package.

## Acceptance criteria

- A1: The admin app's JSON CAPTCHA verification request reaches the authorization action and returns a verification result.
- A2: A valid configured hCaptcha test token is accepted, while an invalid token is rejected.
- A3: The existing Refit integration sends the configured secret, token, and optional client IP in hCaptcha's expected form format and has the request builder required by the running client.

## Non-goals and stop conditions

No changes to password-grant validation, token issuance, user accounts, admin UI, production CAPTCHA credentials, or Aspire port configuration. If sign-in still fails after CAPTCHA verification succeeds, record and investigate that as a separate authorization-flow issue before expanding this change.
