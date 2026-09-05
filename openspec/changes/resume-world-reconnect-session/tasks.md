# Tasks

## Authentication and GameWorld

- [x] Keep dedicated reconnect-only authorization validation separate from
  normal token-issuing sign-in.
- [x] Acknowledge opcode 14, then classify the following authentication request
  before logical Raido context creation.
- [x] Validate exact existing world session, claim, logical connection,
  character, and authentication subject without fresh-login side effects.
- [x] Invoke the per-connection dispatch context for new and existing logical
  destinations; do not return a connection-selection DTO.
- [x] Keep GameWorld preparation inside the existing session claim until the
  internal physical attach completes.

## Raido and protocol

- [x] Preserve the existing #477/#488 Raido reconnect state and route physical
  dispatch through `RaidoConnectionDispatcher`, a scoped application delegate,
  and a per-connection dispatch context.
- [x] Add only the internal derived awaiting-reconnect preflight; add no new
  reconnect-state fields, transitions, reservations, leases, or markers.
- [x] Keep the one internal `TryAttachPhysicalConnection` operation as the
  authoritative final transition.
- [x] Remove candidate-context creation, cross-context transfer,
  response-aware physical writes, and duplicate reconnect completion logic.
- [x] Revalidate the session, claim, target, character, and subject inside the
  existing claim, install the fresh reconnect protocol, flush response 15, and
  attach before releasing the claim.
- [x] Inject request-specific handshake validators for reconnect, fresh world,
  and lobby requests.
- [x] Keep generic handshake framing and response 15's declared two-byte
  length with the exact 4,608-byte payload.
- [x] Preserve the outer handshake cancellation token through claim,
  preparation, response flush, and attach; split replacement-only preparation
  failure from mutation-aware target cleanup.
- [x] Resolve the reconnect handler lazily after reconnect classification while
  retaining the accepted-connection handshake protocol scope.

## Tests and validation

- [x] Preserve decoder, authentication, framing, fresh-login, and Raido attach
  coverage.
- [x] Add focused coverage for scoped dispatcher lifetimes, active-target
  preflight rejection, valid reconnect ordering, claim-serialized concurrent
  candidates, first-packet buffering, attach-failure termination, mutation
  boundary cancellation, claim release, lazy reconnect-handler resolution, and
  protocol lifetime ownership.
- [x] Run strict OpenSpec validation, the requested test matrix, solution
  build, architecture grep, and final diff/scope review.
