# Tasks

## 1. Provider behavior

- [x] 1.1 Add a required-parent decode path for generic and sprite `Get` calls that reuses the already-read reference table and existing snapshot-backed child decoder.
- [x] 1.2 Add regressions for generic `Get`/`GetRange`, sprite missing-parent behavior, child-missing fallbacks, and retained specialized-provider fallback semantics.

## 2. Specification and validation

- [x] 2.1 Sync the canonical cache snapshot specification and verify focused cache tests, strict OpenSpec validation, and `git diff --check`.
