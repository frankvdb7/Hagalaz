# Proposal

## Why

The cache coordinates local writes with single-member snapshot reads and reference-table lookups. `ReadArchive`, `ReadContainer`, and workflows that compose multiple cache operations do not hold that exclusion for their full read and decode sequence. State this boundary explicitly so snapshot-member coherence is not mistaken for operation-wide cache consistency.

## What Changes

- Clarify the `ICacheAPI` concurrency contract and the cache snapshot specification.
- Document that `ReadArchive`, `ReadContainer`, and composed range or inspection workflows are not atomic with concurrent local writes.
- Keep the existing synchronization and runtime behavior unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `cache-archive-member-snapshots`: bound the local write-coherence guarantee to snapshot-member reads and reference-table lookups.

## Acceptance Criteria

- The documented guarantee identifies the operations serialized with same-instance writes.
- The documentation explicitly excludes whole-operation atomicity for `ReadArchive`, `ReadContainer`, and composed range or inspection workflows.
- No runtime synchronization behavior changes.

## Non-goals and Stop Conditions

- Do not widen the reader/writer lock around archive or container reads.
- Do not add locks, retries, generations, or a general consistency framework.
- Do not change file-store serialization or cache mutation behavior.
