# Tasks

## 1. Provider behavior regressions

- [x] 1.1 Add generic and sprite provider tests proving an absent parent mapping uses the factory fallback, and a missing child in an existing parent also falls back; verify the focused `TypeProviderSnapshotTests` pass.
- [x] 1.2 Add generic provider tests proving mapped physical-file absence and mapped corrupt archive failures propagate without factory fallback; verify the focused tests and existing `CacheApiSnapshotTests.Read_PreservesMissingAndCorruptArchiveFailuresAndReleasesReadLock` pass.
- [x] 1.3 Dispose the temporary `MemoryStream` in `RawStoreMutationCoherenceTests.MemoryFileStore.Set`; verify the focused service-cache test project passes.

## 2. Specification and final validation

- [x] 2.1 Sync the canonical cache snapshot specification with the delta and verify `openspec validate --all --strict` plus `git diff --check` pass.

## Workflow follow-up

- Archive this change after all tracked tasks and required validation pass.
