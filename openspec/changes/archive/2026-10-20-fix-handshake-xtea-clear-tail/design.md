## Context

The client implementation's `Buffer.encryptXTEA` loops over `(length - offset) / 8` complete blocks and leaves the remainder untouched. Existing revision-742 captures record a 479-byte XTEA section, so the final seven bytes are clear. The server helper copied the full section safely but rejected it before decryption when its length was not divisible by eight.

## Decision

Retain the current exact-length copy and cleanup owner. Require at least one complete XTEA block, copy the complete unread section into the rented buffer, and call the existing `XTEA.Decrypt` over that exact buffer. The BCL-compatible implementation already decrypts only complete blocks and leaves a remainder unchanged. The parser receives the exact copied length, so no rented capacity is exposed.

This preserves the security boundary from the earlier hardening while matching the client's actual wire contract. A payload shorter than one block remains invalid because it provides no encrypted block and cannot form a valid authentication request.

## Verification Strategy

- Unit-test one complete encrypted block plus a seven-byte clear tail and assert the parser sees the original bytes.
- Exercise both production handshake decoders with a valid payload plus a clear tail.
- Run the focused handshake tests, the complete GameWorld test project, the affected build, strict OpenSpec validation, and the manual client login boundary.
