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

- [x] Preserve the existing #477/#488 Raido reconnect state machine and use a
  single thin wrapper over its existing physical attach API.
- [x] Remove candidate-context creation, cross-context transfer, transfer
  methods, response-aware physical writes, runtime reconnect features, and
  duplicate reconnect completion logic.
- [x] Install the fresh reconnect protocol on the existing target before
  sending response 15 and before the client can send game input.
- [x] Keep generic handshake framing and response 15's declared two-byte
  length with the exact 4,608-byte payload.

## Tests and validation

- [x] Preserve decoder, authentication, framing, fresh-login, and Raido
  attach coverage; add focused coverage for the raw classification and direct
  target attach boundary where the existing test seams permit.
- [x] Run strict OpenSpec validation, the requested test matrix, solution
  build, and final diff/scope review.
