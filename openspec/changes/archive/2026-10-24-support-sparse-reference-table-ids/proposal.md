# Proposal

## Why

Reference tables encode archive IDs independently from the number of groups. The server currently uses a backing collection capacity as an archive-ID limit, so a valid sparse group can be rejected before its entry is looked up.

## What Changes

- Define `IReferenceTable.Count` as the number of present archive groups and `Capacity` as one greater than the greatest present archive ID, or zero for an empty table.
- Preserve sparse archive IDs through codec round trips, table copies, and name lookup.
- Allow cache reads to reach present sparse groups while continuing to report absent group IDs as missing.
- Report both count and ID-space capacity in the reference-table response.

## Capabilities

### New Capabilities

- `cache-reference-tables`: Reference-table group counts, sparse IDs, and lookup behavior.

### Modified Capabilities

None.

## Scope

### In Scope

- The reference-table model and interface.
- Reference-table codec count handling.
- Cache API archive lookup guards and the reference-table response.
- Focused model, codec, and cache-read regressions.

### Non-Goals

- Changes to the GameClient or the cache wire format.
- Changes to physical member indexing within an archive.
- Changes to archive snapshot retention or cache mutation coherence.

## Acceptance Criteria

- A table with group IDs 1 and 100 reports Count 2 and Capacity 101.
- Encoding and decoding that table preserves both IDs and its group count.
- Cache API `Read` and `ReadArchive` can read group 100 and still reject an absent ID such as 50.
- The reference-table response exposes both Count and Capacity.

## Stop Conditions

- Stop if the fix requires changing the cache wire format or GameClient behavior.
- Stop if implementation requires a cache owner, event, or invalidation mechanism.

## Impact

Changes are limited to `Hagalaz.Cache.Abstractions`, `Hagalaz.Cache`, the Cache service reference-table response, and their focused tests and OpenSpec capability.
