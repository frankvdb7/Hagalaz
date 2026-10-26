## MODIFIED Requirements

### Requirement: Mapped definitions decode through the cache API

Providers MUST decode mapped physical members through `ICacheAPI.Read` and propagate missing-file or decode failures. Existing custom-provider delegation and post-decode behavior MUST remain intact. A null-terminated string that reaches the end of its input without a terminator MUST fail with `EndOfStreamException`.

#### Scenario: A provider decodes a present definition

- **WHEN** a provider reads a present definition
- **THEN** it decodes the mapped physical member into a fresh definition and invokes existing post-decode behavior once

#### Scenario: A mapped archive file is missing or corrupt

- **WHEN** a provider requests a mapped definition but its archive file is missing or its data cannot be decoded
- **THEN** the cache's file-not-found or decoding failure propagates and the provider does not use a factory fallback

#### Scenario: A mapped definition contains an unterminated string

- **WHEN** a definition codec reads a null-terminated string whose terminator is missing
- **THEN** decoding fails with `EndOfStreamException` after consuming the available bytes
