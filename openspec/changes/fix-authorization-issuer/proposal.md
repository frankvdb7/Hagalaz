## Why

The live revision-742 login reaches the authorization consumer, but OpenIddict
cannot create tokens from that MassTransit context because no HTTP request exists
from which to infer the issuer. GameWorld maps the resulting sign-in failure to
the client's `bad session id` response.

## What changes

- Configure one explicit OpenIddict issuer for token creation and validation.
- Derive a local Development issuer from the HTTPS or HTTP launch-profile port
  when no explicit value is supplied.
- Require a configured absolute issuer outside Development and require HTTPS in
  those environments.
- Add focused configuration regression tests and document the setting.

## Impact

This affects authorization-service OpenIddict configuration only. It changes no
credential validation, token claims, client protocol, session ownership, or
reconnect behavior.

## Acceptance criteria

- MassTransit token creation has a valid issuer in the Aspire Development stack.
- Production startup rejects a missing or non-HTTPS issuer.
- Explicit configured issuers are used unchanged apart from OpenIddict's normal
  URI handling.
- Existing authorization tests and the real login path can proceed past token
  creation.

## Non-goals

- Do not change credentials, token claims, signing keys, or OpenIddict flows.
- Do not change the GameWorld `BadSession` response mapping.
- Do not change the client or reconnect transport.
