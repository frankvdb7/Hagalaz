# Proposal

## Why

Single-definition providers now return a factory fallback when the reference table has no mapping for the requested definition, including when its logical parent archive is absent. This is a valid missing-definition behavior, but the cache snapshot specification currently conflates an absent logical mapping with a missing or corrupt archive file.

## What Changes

- Clarify that no reference-table mapping for a definition uses the provider factory fallback, whether the archive is absent or the child is absent from an existing archive.
- Clarify that a present mapping whose backing archive is missing or corrupt continues to propagate the cache read/decode failure.
- Add focused generic and sprite provider regression coverage for these cases.
- Dispose the temporary stream owned by the raw-mutation test store helper.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `cache-archive-member-snapshots`: distinguish missing logical definitions from missing/corrupt mapped archive data.

## Impact

This change clarifies and tests existing provider behavior. It does not change production code, cache read exceptions, range-read behavior, or provider hooks. Tests use the existing cache API boundary and reference-table model; no new dependencies or abstractions are needed.

## Acceptance Criteria

- A generic provider returns a factory fallback when the reference table has no parent archive entry.
- A generic provider returns a factory fallback when a mapped parent archive lacks the requested logical child.
- A provider propagates `FileNotFoundException` or `InvalidDataException` when a logical child is mapped but its archive read fails.
- The sprite provider retains the same missing-parent fallback behavior.
- The raw-mutation test store helper disposes the temporary stream it creates.
- The canonical specification distinguishes these outcomes and strict OpenSpec validation passes.

## Non-goals and Stop Conditions

- Do not restore eager `ReadArchive` calls or change cache-level missing-file/corruption exceptions.
- Do not change `GetRange`, custom provider delegation, event-hook frequency, or factory implementation.
- Stop if achieving the acceptance criteria requires changing the `ICacheAPI` or type-provider production contracts.
