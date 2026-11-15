# Proposal

## Why

Cache Studio can browse CS2 script IDs and show definition metadata. The detail page needs disassembly so administrators can inspect what the cached script contains.

## What Changes

- Give the existing CS2 scripts tab a script-specific table with ID, name when present, and instruction count. Keep its pagination and numeric lookup.
- Give the existing script route a read-only detail page with metadata, ordered revision-742 instructions, and switch tables. Use the Cache service's authenticated type routes and existing admin request-state patterns.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `cache-type-details` gains requirements for browsing and inspecting CS2 scripts.

## Impact

The Angular admin and Cache service need a typed contract for one script's disassembly. The Cache library needs revision-742 decoding suitable for inspection.

## Acceptance and stop conditions

An administrator can open a script from the tab or by direct URL, read metadata and disassembly from the same cache record, and distinguish missing scripts from failed reads. Verify the revision-742 instruction stream against representative fixtures and cover list-to-detail navigation with a focused regression test. That completes the change. Editing, execution, source decompilation, and the CS2 integers tab are outside scope.
