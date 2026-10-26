# Proposal

## Why

Cache definition strings use a null-terminated single-byte encoding. The Cache service currently accepts object names containing the terminator or characters outside Latin-1, allowing an edit to truncate or alter stored text.

## What Changes

- Make cache string writes preserve Latin-1 characters and reject null terminators or unrepresentable characters.
- Validate object-name requests before reading or writing cache entries.
- Add binary string and HTTP mutation regressions.

## In Scope

Prevent lossy or structurally invalid strings from being persisted by object-name edits.

## Non-Goals

Do not change the cache's single-byte string format or add Unicode transcoding.

## Capabilities

### Modified Capabilities

- `cache-definition-mutations`: Object-name edits accept only representable cache strings and reject invalid input without modifying the archive.

## Acceptance Criteria

1. Latin-1 characters, including high-byte values, survive string and object-definition round trips.
2. Object-name requests containing an embedded null or a character outside Latin-1 return HTTP 400 without changing archive or reference-table bytes.

## Stop Conditions

Stop if supporting arbitrary Unicode requires changing the cache's stored string format.
