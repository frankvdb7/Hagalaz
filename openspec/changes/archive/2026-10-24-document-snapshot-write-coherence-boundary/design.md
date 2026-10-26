# Design

## Context

`CacheApi.ReadReferenceTable` and the single-member `Read` hold the snapshot reader lock while they use the reference table or snapshot. `CacheApi.Write`, `WriteRawFile`, and `CopyRawFile` take the writer lock. In contrast, `ReadArchive` obtains a table through `ReadReferenceTable`, releases that lock, then reads and decodes a container. `ReadContainer` reads the file and decodes it without holding the snapshot lock. A range or inspection workflow can therefore span several separately synchronized operations while a local mutation proceeds.

## Decision

Document the current boundary at the `ICacheAPI` contract and in the living cache snapshot specification. Member reads and reference-table lookups remain serialized with same-instance local mutations. Higher-level archive, container, range, and inspection operations receive no whole-operation before-or-after guarantee when they overlap a mutation.

Keep runtime code unchanged. Widening the lock would introduce nested calls under a `NoRecursion` lock and increase contention for operations that #506 does not need to make atomic. Callers that require operation-wide consistency across mutation and inspection need a separate coordination decision.

## Non-goals

- No synchronization, transaction, or cache-layer changes.
- No guarantee for external processes or direct file-store access.
