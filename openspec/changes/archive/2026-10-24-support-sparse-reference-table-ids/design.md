# Design

## Context

See `proposal.md` for the failure path and `specs/cache-reference-tables/spec.md` for the behavior contract. The decoded reference table already stores archive entries by ID in a `SortedList`; the conflation is between that sparse key space and the collection's storage capacity.

## Goals / Non-Goals

**Goals:**

- Keep group count and archive-ID capacity as distinct model values.
- Preserve the existing cache reference-table wire format.
- Make sparse IDs addressable through existing cache reads and the reference-table endpoint.

**Non-Goals:**

- Change archive member physical indexes or `ReferenceTableEntry.Capacity`.
- Modify GameClient or cache snapshot/mutation behavior.

## Decisions

1. **Expose `Count` and give `Capacity` ID-space semantics.** `Count` is the number of stored entries. `Capacity` is derived from the largest stored archive key plus one. Actual lookup remains key-based so gaps inside the capacity are not treated as present.

2. **Use group count in the wire codec.** The decoder retains the header's `fileCount` in a local variable while reading IDs, since the table has no stored entries yet. The encoder writes `Count`, never ID-space capacity. This avoids changing the on-disk format.

3. **Enumerate present entries for name lookup and copies.** These operations visit the stored entries instead of iterating across the ID-space capacity, keeping sparse high IDs efficient and intact.

4. **Expose both values in the reference-table response.** Add `Count` while retaining `Capacity`; repository search found no in-tree consumer of the response that assumes capacity is a group count.

5. **Keep cache reads membership-aware.** `CacheApi` may use the exclusive capacity bound, then uses `GetEntry` to reject holes. Negative IDs continue to fail as missing.

## Risks / Trade-offs

- **[Risk]** The default `Count` implementation enumerates entries for custom `IReferenceTable` implementations. **Mitigation:** `ReferenceTable` provides a direct collection count, while the default preserves source compatibility for other implementations.
- **[Risk]** A codec that writes `Capacity` would corrupt sparse tables. **Mitigation:** Encode from `Count` and add sparse round-trip coverage.
- **[Risk]** The response gains a JSON property. **Mitigation:** `Count` is additive; existing fields and archive identifiers remain available.
