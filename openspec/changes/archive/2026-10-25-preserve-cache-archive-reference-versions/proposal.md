# Proposal

## Why

`CacheWriter` stores rewritten archives without a physical container version trailer. After the first rewrite, decoding the archive therefore yields container version `-1`; deriving the next archive version from that container resets the reference-table entry instead of advancing it.

## What Changes

- Advance an existing archive's reference-table version from its current entry value on each high-level write.
- Preserve the existing physical container encoding convention.
- Cover two consecutive archive-member replacements with the real cache writer, codecs, and in-memory file store.

## Non-goals

- Do not add a version coordinator or a new write abstraction.
- Do not change the physical container trailer convention or make the multi-file write transactional.

## Acceptance Criteria

- Two consecutive rewrites starting from reference-table version 5 leave the entry at version 7.
- Rewritten physical containers retain the existing trailer-free encoding and the final member bytes remain readable.

## Stop Conditions

- If representative cache data requires a different physical trailer convention, handle that as a separate format-compatibility change.
