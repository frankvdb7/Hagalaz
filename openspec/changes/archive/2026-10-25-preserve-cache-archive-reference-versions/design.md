# Design

`CacheWriter` owns archive and reference-table metadata updates. The existing `ReferenceTableEntry.Version` is the authoritative version source; increment it directly for each rewrite. The writer continues to serialize the physical container with `Encode(false)`, so version metadata remains in the reference table and the on-disk container convention does not change.

The regression uses `CacheApi.ReplaceArchiveMember` with the real `CacheWriter`, `ContainerDecoder`, archive codecs, and `ReferenceTableCodec`. It seeds a versioned archive and then performs two replacements, proving the second update advances from the persisted reference-table version even though the intermediate physical container has no trailer.

Rejected alternatives: copying the decoded container version preserves the reset; adding a version coordinator or changing the container trailer format introduces a second mechanism or changes the physical format without evidence that it is needed.
