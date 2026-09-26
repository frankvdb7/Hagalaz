## 1. Correct the request-side protocol contract

- [x] 1.1 Remove the stale authentication-protocol opcode-18 decoder registration while retaining opcode 16.
- [x] 1.2 Rename the decoder boolean to `isReconnect` and select `WorldSignInRequest` for flag 0 or `WorldReconnectRequest` for flag 1 through the existing parser.
- [x] 1.3 Add the minimal sealed `WorldReconnectRequest` message with no handler, response, or orchestration.

## 2. Add focused regression coverage

- [x] 2.1 Extend GameWorld handshake decoder tests for contiguous and segmented flag-1 input, while preserving flag-0 and malformed-input coverage.
- [x] 2.2 Extend the generic Raido dispatcher tests with BaseMessage/DerivedMessage exact-type dispatch coverage and no GameWorld references.
- [x] 2.3 Update characterization tests and fixture assertions for the verified request contract, registry absence, qualified observations, unknown production behavior, and secret omission.

## 3. Reconcile OpenSpec and PR metadata

- [x] 3.1 Rename the change to `preserve-world-reconnect-intent` and update proposal, design, specification, README, and task references.
- [x] 3.2 Update PR #487 title/body to describe the request-side correction, characterization coverage, deferred reconnect implementation, and open #478 status.

## 4. Validate the bounded change

- [x] 4.1 Run focused tests, full GameWorld tests, full Raido tests, solution build/test commands, strict OpenSpec validation, and `git diff --check`; solution integration failures were limited to unavailable Docker/Testcontainers infrastructure.
- [x] 4.2 Review the effective diff against `main` and verify no response, session, cipher, transport, lifecycle, or resynchronization behavior was added.
