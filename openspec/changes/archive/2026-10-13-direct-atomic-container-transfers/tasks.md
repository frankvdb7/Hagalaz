# Tasks

## 1. Boundary transfer ownership

- [x] 1.1 Add shared current-thread transaction detection and direct ordered standalone transfer ownership to `ItemContainerMutationBoundary`; verify with focused standalone and transaction participation tests.
- [x] 1.2 Add retained-ownership publication without changing single-boundary publication; verify publisher order, lock release, claim lifetime, and cleanup after failure.

## 2. Container and Equipment integration

- [x] 2.1 Replace implicit transaction creation in `ItemContainer.TryTransferTo` and update its XML documentation; verify standalone, explicit transaction, rejection, rollback, and concurrency tests.
- [x] 2.2 Replace the standalone transaction in `EquipmentContainer.TryMoveTo` with boundary ownership and under-lock validation; verify full/partial removal lifecycle order and hook/publication failures.

## 3. Specification and validation

- [x] 3.1 Update canonical transfer and Equipment requirements and document both public execution modes; verify OpenSpec strict validation and search for obsolete short-transaction claims.
- [x] 3.2 Run focused Abstractions and GameWorld tests, relevant Game Scripts tests, the solution build and tests, the duplication gate, and `git diff --check`; record unavailable checks and failures.

Validation note: focused Abstractions (63/63), GameWorld (56/56), and trade/duel Scripts (65/65) tests passed. The full solution build passed. The full solution test run had 23 Docker-backed integration tests fail to start because Docker was unavailable at `npipe://./pipe/docker_engine`; the other test projects passed. The duplication gate found no new clones, strict OpenSpec validation passed (69/69), and `git diff --check` passed.
