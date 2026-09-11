## 1. Planning and production simplification

- [x] 1.1 Record the ownership boundaries and validate the change artifacts with strict OpenSpec validation.
- [x] 1.2 Remove `CreatureUpdateState` and `TryBeginClientUpdate`, guard ticks with `IsDestroyed`, and update character rendering without changing worker phase order.
- [x] 1.3 Replace Dimension concurrent residency collections with ordinary dictionaries and safe snapshots while preserving exact service transitions.
- [x] 1.4 Replace ContactSessionStore's concurrent dictionary plus lock with one locked ordinary dictionary and snapshot enumeration.
- [x] 1.5 Remove MapRegion teardown-only internal collection cleanup and `MapRegionPart.RemoveDestroyed` while preserving external cleanup and terminal behavior.
- [x] 1.6 Remove the redundant `IGameSessionStore` lookup from GameSessionAbortCoordinator and simplify NpcService rethrows without changing exception behavior.

## 2. Regression coverage

- [x] 2.1 Add creature tick failure-recovery and destroyed-creature no-op tests.
- [x] 2.2 Add residency snapshot and exact-instance regression tests.
- [x] 2.3 Add ContactSessionStore generation, stale-removal, and snapshot tests.
- [x] 2.4 Update MapRegion teardown tests to assert external ownership cleanup rather than dead-collection tidiness.
- [x] 2.5 Preserve and verify session-abort retry/reservation and MapRegionPart update-buffer tests.

## 3. Validation

- [x] 3.1 Run focused GameWorld and Contacts tests with the repository-compatible `dotnet test` commands.
- [x] 3.2 Run the GameWorld integration tests where Docker infrastructure permits and report infrastructure failures separately.
- [x] 3.3 Run the Raido tests, solution build, strict OpenSpec validation, and final diff checks.
