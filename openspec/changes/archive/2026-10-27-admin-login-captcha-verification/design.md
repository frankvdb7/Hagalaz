# Design

## Context

See proposal.md for the failure path. The admin SPA sends a JSON request to a public, stateless CAPTCHA verification action. `HCaptchaService` owns the provider call and already receives its endpoint and credentials from `CaptchaOptions`; the authorization project already references Refit with its source generator.

## Goals / Non-Goals

**Goals:** Keep the JSON admin endpoint contract, have the existing Refit client send hCaptcha's URL-encoded fields, and ensure that request is generated without reflection.

**Non-Goals:** Do not change credential sign-in, token issuance, account state, provider configuration ownership, or the web app's CAPTCHA UI.

## Decisions

- Remove the MVC antiforgery authorization filter from the CAPTCHA action. This endpoint is stateless and does not authenticate or mutate browser-cookie state; the admin client posts JSON and does not issue an antiforgery token. Registering MVC view-feature antiforgery services or adding a second token exchange would not match the endpoint's contract.
- Represent the hCaptcha request as one URL-encoded Refit body containing `secret`, `response`, and `remoteip` only when available. This matches the provider contract and a form-body shape supported by Refit's source generator. Keep `HCaptchaService` responsible for building the provider fields from existing options and request inputs.
- Keep the current `AddRefitClient<IHCaptchaApi>` registration and add `Refit.Reflection` at the same version as Refit. The rebuilt service still calls `RequestBuilder.ForType<IHCaptchaApi>` and fails because no generated factory is registered; Refit 14 moved that supported runtime fallback into the optional package. Fix the form contract as well so the fallback sends the provider's required content type. Forcing a generated client would add build-specific wiring without an existing generated factory; a separate `HttpClient` implementation would duplicate the existing Refit integration.
- Treat hCaptcha's `hostname` response property as optional. The provider omits it when rejecting a token, and requiring it prevents the service from returning the ordinary failure result.
- Keep test credentials in local Development user secrets. Do not add provider secrets or test-key defaults to repository configuration.

## Risks / Trade-offs

- The runtime request builder uses reflection and is unsuitable for trimming or Native AOT. This authorization service is not currently configured for either; if that changes, replace the fallback with a verified generated client.
- A provider request with missing or invalid configured credentials will still be rejected by hCaptcha. Use the existing Development user-secrets configuration and verify with hCaptcha's published test values.
- The endpoint remains public and stateless, so an unsolicited request can consume a supplied CAPTCHA token. It creates no authenticated or persisted state; the existing provider decides whether a token is valid.

## Migration Plan

No data migration is required. Rebuild and restart the authorization resource through Aspire, then exercise the endpoint through the admin proxy with the published test token. Roll back by reverting the controller and request-contract edits.
