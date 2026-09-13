## Context

The revision-742 client reads each archive chunk footer value as a delta from
the previous file and accumulates it to obtain the current file's chunk size.
The total file size is the sum of those cumulative chunk sizes. The server
currently performs the first accumulation, then subtracts the previous file
again when allocating and copying members.

## Decision

Keep the existing `ArchiveDecoder` and `IArchive` boundary. Store the
cumulative chunk sizes, sum them directly for each member's capacity, and copy
each cumulative chunk-size segment while advancing the source by that segment.
Validate the chunk count, footer bounds, non-negative cumulative sizes, and
source consumption before allocating or copying member data.

This matches the client parser and keeps malformed cache data from reaching
`MemoryStream` with a negative capacity.

## Verification strategy

- Use a one-chunk archive whose footer contains a negative delta while all
  cumulative sizes remain valid.
- Use a multi-chunk archive and assert every member's reconstructed bytes.
- Exercise the decoder's malformed-footer guard.
- Run focused cache tests, the affected build, strict OpenSpec validation, and
  the manual world-entry action.
