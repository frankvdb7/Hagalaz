# Tasks

## Authentication and GameWorld

- [x] Keep dedicated reconnect-only authorization validation separate from
  normal token-issuing sign-in.
- [x] Acknowledge opcode 14, then classify the following authentication
  request before logical Raido context creation.
- [x] Validate exact existing world session, claim, logical connection,
  character, and authentication subject without fresh-login side effects.
- [x] Resolve the existing logical target, return it to Raido connection
  infrastructure, and update only reconnect client metadata.

## Raido and protocol

- [x] Preserve the existing #477/#488 Raido reconnect state machine and route
  physical dispatch through `RaidoConnectionDispatcher`, which delegates the
  one final attach to the existing internal seam.
- [x] Remove candidate-context creation, cross-context transfer, transfer
  methods, response-aware physical writes, runtime reconnect features, and
  duplicate reconnect completion logic.
- [x] Revalidate the session, claim, target, character, and subject inside the
  existing claim, install the fresh reconnect protocol, flush response 15, and
  return the target for the dispatcher to perform the existing single raw
  transport attach. Abort the target if that final attach loses the narrow
  post-response race.
- [x] Inject request-specific handshake validators for reconnect, fresh world,
  and lobby requests. Keep reconnect failure mapping and target ownership
  checks local to the reconnect handler.
- [x] Keep generic handshake framing and response 15's declared two-byte
  length with the exact 4,608-byte payload.

## Tests and validation

- [x] Preserve decoder, authentication, framing, fresh-login, and Raido
  attach coverage; add focused coverage for raw classification, explicit raw
  authentication metadata, claim-serialized winner-before-mutation, stale
  duplicates, first-packet buffering, attach-failure termination, and
  protocol lifetime ownership. Existing Raido transport tests cover the
  single-transition reconnect state machine.
- [x] Run strict OpenSpec validation, the requested test matrix, solution
  build, and final diff/scope review.
