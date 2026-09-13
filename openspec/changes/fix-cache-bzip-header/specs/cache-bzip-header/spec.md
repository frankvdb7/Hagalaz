## ADDED Requirements

### Requirement: Decode standard and cache-style BZip2 payloads

The cache compression utility MUST decode complete BZip2 streams and cache
payloads whose leading `BZh1` stream marker has been removed.

#### Scenario: A complete BZip2 stream is decoded

- **WHEN** the utility receives a normal BZip2 stream
- **THEN** it MUST return the original bytes

#### Scenario: A headerless cache payload is decoded

- **WHEN** the utility receives a cache-style payload beginning at the BZip2
  block data
- **THEN** it MUST restore the `BZh1` marker and return the original bytes,
  including payloads spanning multiple BZip2 blocks

#### Scenario: A truncated cache payload is rejected

- **WHEN** the utility receives an invalid or truncated headerless payload
- **THEN** it MUST fail instead of returning partial or fabricated data

### Requirement: Preserve container metadata

The container decoder MUST preserve the decoded compression type, version, and
uncompressed content when the container carries a headerless BZip2 payload.

#### Scenario: A headerless BZip2 container is decoded

- **WHEN** a container contains a cache-style BZip2 payload with a version
  byte
- **THEN** the decoder MUST preserve the compression type, version, and exact
  uncompressed content
