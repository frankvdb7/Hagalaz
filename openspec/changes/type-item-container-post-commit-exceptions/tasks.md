# Tasks

## 1. Typed transaction completion failures

- [x] 1.1 Add the two sealed completion/publication exception types; verify each directly derives from `Exception`, preserves the exact inner exception, and documents that storage is irreversible.
- [x] 1.2 Wrap only transaction-owned completion and publication callbacks in `Commit()`; verify infrastructure and pre-commit exceptions remain unwrapped and callback order/fail-fast behavior is unchanged.
- [x] 1.3 Add transaction regressions for completion failure, publication failure, aggregate inner exceptions, rejected operations, and pre-commit rollback; verify committed state, disposal cleanup, and no callback retry.
- [x] 1.4 Update transaction-owned publisher/equipment exception assertions in GameWorld, Scripts, and trade tests; verify each expects the phase-specific type and exact original exception.

## 2. Duel and trade integration

- [x] 2.1 Handle the two post-commit exception types with one exception filter at the duel victory boundary and log the failure while continuing the existing victory flow; verify real death completion finishes without duplicate payout and capacity rejection remains retryable.
- [x] 2.2 Verify trade completion/refund stays terminal when transaction publication throws and the original exception is retained by `ItemContainerPostCommitPublicationException`.

## 3. Specifications and validation

- [x] 3.1 Update canonical item-container and trading requirements plus the active duel settlement change to describe phase-based classification, including deferred Equipment hook failures; verify deltas match canonical behavior.
- [x] 3.2 Run focused Abstractions, GameWorld, and Scripts tests, then solution build and full solution tests; record any environment-blocked checks.
- [x] 3.3 Run `openspec validate --all --strict`, the repository duplication gate, and `git diff --check`; inspect the final cumulative diff for scope and warning regressions.

## Validation notes

- For the phase-specific exception refinement, focused tests passed: Game Abstractions (356), GameWorld (1,336), and Game Scripts (378).
- `dotnet build Hagalaz.sln --no-restore --no-incremental --verbosity quiet` passed with 0 errors. Existing solution warnings remain; warnings in changed C# files point to unchanged lines, and no warning is attributed to the new exception types or the new API-shape test.
- `openspec validate --all --strict` passed. jscpd 5.3.2 found 502 baseline clones and no new clone; `git diff --check` passed.
- The full solution test run was performed for the preceding implementation at `8859a223817f54b2550debbbb64894f4bcdd08ef`: 23 tests in `Hagalaz.Services.Characters.Tests`, `Hagalaz.Data.IntegrationTests`, and `Hagalaz.Services.GameWorld.IntegrationTests` failed because Testcontainers could not connect to Docker at `npipe://./pipe/docker_engine`. The focused suites passed on that run as well. The full solution test run was not repeated for this API-only refinement.
