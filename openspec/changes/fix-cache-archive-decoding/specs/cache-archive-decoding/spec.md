## ADDED Requirements

### Requirement: Decode multi-file cache archives using cumulative chunk sizes

The cache service MUST decode revision-742 multi-file archive metadata by
accumulating each footer delta within a chunk and using the resulting
cumulative value as that file's chunk length.

#### Scenario: Decode valid negative footer deltas

- **WHEN** an archive footer contains a negative delta after a larger previous
  file while all cumulative chunk sizes remain non-negative
- **THEN** the decoder returns the member files with their declared contents
  and does not allocate a negative stream

#### Scenario: Decode multiple chunks

- **WHEN** an archive contains more than one data chunk
- **THEN** the decoder sums each member's cumulative chunk lengths and
  reconstructs each member in chunk order

#### Scenario: Reject an invalid footer

- **WHEN** archive metadata produces a negative cumulative size or exceeds the
  available data region
- **THEN** the decoder rejects the archive with an invalid-data exception
