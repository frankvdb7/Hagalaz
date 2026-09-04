# Tasks

## Authentication and GameWorld

- [x] Keep dedicated reconnect-only authorization validation separate from
  normal token-issuing sign-in.
- [x] Classify the raw first handshake before logical Raido context creation.
- [x] Validate exact existing world session, claim, logical connection,
  character, and authentication subject without fresh-login side effects.
- [x] Attach the raw reconnect connection to the existing logical target and
  update only reconnect client metadata.

## Raido and protocol

- [x] Preserve the existing #477/#488 Raido reconnect state machine and call
  its existing physical attach API directly. Make only the existing seam
  public because the GameWorld assembly boundary requires it.
- [x] Remove candidate-context creation, cross-context transfer, transfer
  methods, response-aware physical writes, runtime reconnect features, and
  duplicate reconnect completion logic.
- [x] Install the fresh reconnect protocol on the existing target, flush
  response 15, and only then attach the raw transport so immediate game input
  remains buffered until Raido resumes.
- [x] Inject request-specific handshake validators for reconnect, fresh world,
  and lobby requests. Keep reconnect failure mapping and target ownership
  checks local to the reconnect handler.
- [x] Keep generic handshake framing and response 15's declared two-byte
  length with the exact 4,608-byte payload.

## Tests and validation

- [x] Preserve decoder, authentication, framing, fresh-login, and Raido
  attach coverage; add focused coverage for raw classification and injected
  reconnect validation. Existing Raido transport tests cover buffered input,
  rejected attach, and protocol lifetime behavior.
- [x] Run strict OpenSpec validation, the requested test matrix, solution
  build, and final diff/scope review.
