## 1. Characterize the observed reconnect contract

- [x] 1.1 Materialize the available GameClient #142 controlled-peer evidence as a local, secret-safe fixture and characterization test: handshake 14, opcode 16 with flag 0 for fresh login, opcode 16 with flag 1 for reconnect, response 15, the 4,608-byte `readEnterWorldPacket(true)` payload, authentication reset, protocol preservation, new client/server ISAAC instances, temporary-key clearing, RSA/XTEA boundaries, the server-key `+50` transform, and the client-observed event order; record that server physical-adoption and resynchronization ordering is unavailable rather than inventing it, and verify exact safe lengths, offsets, hashes, and fields without storing generated secrets
- [x] 1.2 Document and test that client login state 18 is not wire opcode 18, that wire opcode 18 is not the active GameWorld reconnect contract, and that the implementation does not infer reconnect from `loginType == 9` alone; remove the obsolete opcode-18 GameWorld decoder registration

## 2. Implement the characterized handshake contract

- [x] 2.1 Add `WorldReconnectRequest` selected by opcode 16 reconnect flag 1, preserve flag 0 as `WorldSignInRequest`, remove the obsolete opcode-18 GameWorld decoder registration, and verify dispatch, malformed input, and segmented input tests
- [x] 2.2 Add a distinct response-15 message and encoder using only the checked-in characterized payload; verify the response code and exact safe fixture fields while leaving the fresh response-2 encoder unchanged
- [ ] 2.3 Implement only the characterized authentication reset, protocol-preserving transition, new client/server ISAAC setup, temporary-key cleanup, RSA/XTEA boundaries, server-key `+50` transform, response/world-entry payload, and fixture-defined ordering; verify no guessed cipher or resynchronization behavior is emitted

## 3. Add transport-only Raido handoff

- [x] 3.1 Add the smallest transport-generic one-shot rebind reservation needed to adopt replacement physical transport ownership and preserve the consumed/unread input boundary; verify concurrent winner selection and old-context ownership
- [x] 3.2 Update the Raido handler to advance the temporary reader before handoff, resume the old logical reader at the preserved boundary, and keep protocol/cipher/lifecycle/completion ownership in the old context; verify no double reader and no premature normal disconnect
- [x] 3.3 Make successful temporary-context cleanup explicit: keep only the old context in `RaidoConnectionStore`, unregister the temporary context, skip temporary `OnDisconnected`/GameWorld signout, and do not abort or dispose the adopted transport; verify each condition

## 4. Reuse GameWorld authentication and session ownership

- [ ] 4.1 Refactor the existing credential/identity validation into a reconnect-safe reuse path for opcode 16 with reconnect flag 1 without creating a GameSession, hydrating/registering a character, replacing ownership, or publishing fresh-login commands; verify side-effect-free authentication
- [ ] 4.2 Define and test temporary token/feature ownership and cleanup on rejection, pre-adoption failure, successful handoff, expiry, and terminal cleanup
- [ ] 4.3 Resolve the existing session through master ID and connection ID, validate the exact GameSession and ICharacter instances, and reuse them without a second registry or GameSession/proxy synchronization mechanism

## 5. Integrate and validate the world reconnect flow

- [ ] 5.1 Wire only GameWorld stateful reconnect for opcode 16 with reconnect flag 1; verify fresh opcode-16 flag-0 behavior, unsupported opcode-18 behavior, unchanged GameUpdate/lobby paths, and exact response-to-resync ordering
- [x] 5.2 Run `dotnet test Raido.Server.Tests\\Raido.Server.Tests.csproj --no-restore` and `dotnet test Hagalaz.Services.GameWorld.Tests\\Hagalaz.Services.GameWorld.Tests.csproj --no-restore`, then run the solution build and verify clean exits
- [x] 5.3 Run `openspec validate resume-world-reconnect-session --type change --strict`, review the cumulative diff for scope/ownership regressions, and record final test evidence
