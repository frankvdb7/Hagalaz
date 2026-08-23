## 1. Protocol messages and decoder

- [x] 1.1 Add a typed world reconnect request and refactor the existing world decoder's message construction so opcode 16 and opcode 18 preserve distinct intent; verify with decoder tests that all shared revision-742 fields remain equal
- [x] 1.2 Register the reconnect-specific decoder for opcode 18 while retaining the existing opcode 16 registration; verify malformed reconnect payloads return `false` with a null message

## 2. Application dispatch

- [x] 2.1 Add the reconnect handler branch that returns the existing failed sign-in response and aborts without authentication; verify the handler is registered for the reconnect message

## 3. Validation

- [x] 3.1 Run the focused GameWorld test project and verify the existing opcode-16 login tests plus new opcode-18 tests pass
- [x] 3.2 Validate the OpenSpec change with `openspec validate preserve-world-reconnect-intent --type change --strict` and verify the diff contains no session-resume or invented-wire-field implementation
