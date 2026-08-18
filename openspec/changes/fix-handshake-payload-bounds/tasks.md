## 1. Shared handshake bounds and ownership

- [x] 1.1 Make packet-header and RSA parsing use exact declared/written lengths and clear used pooled ranges.
- [x] 1.2 Add a shared XTEA payload helper that copies the complete unread sequence, rejects invalid block lengths, exposes only the exact decrypted range, and clears it on every exit path.
- [x] 1.3 Guard settings and hardware variable advances so malformed lengths fail closed.

## 2. Decoder integration and regression coverage

- [x] 2.1 Route world and lobby handshake decoders through the shared bounded XTEA helper without changing successful sign-in fields.
- [x] 2.2 Add focused tests for exact packet lengths, contiguous versus multi-segment encrypted input, stale/unwritten capacity, non-aligned payloads, truncated fields/cache CRCs, and pooled cleanup.

## 3. Verification

- [x] 3.1 Run the focused `Hagalaz.Services.GameWorld.Tests` handshake tests and the project test suite.
- [x] 3.2 Build the affected project and review the final diff for scope, failure handling, and sensitive-buffer cleanup.
