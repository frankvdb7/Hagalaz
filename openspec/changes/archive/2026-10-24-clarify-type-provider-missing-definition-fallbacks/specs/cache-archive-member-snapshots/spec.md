# Spec Delta

## MODIFIED Requirements

### Requirement: Single-member reads return independent read-only member streams

Direct cache API reads MUST serve a requested physical archive member from decoded member bytes and MUST return a fresh, seekable, read-only stream positioned at the start of that member. They MUST preserve the established missing-index, missing-archive, missing-physical-member, and corrupt-data failures.

#### Scenario: Repeated reads of one member

- **WHEN** a caller reads the same physical member more than once
- **THEN** each call returns an independently positioned stream that cannot modify or expose shared member storage

#### Scenario: A decoded archive has multiple members

- **WHEN** callers request different physical members from the same archive
- **THEN** each stream contains only the requested member's bytes

#### Scenario: A direct physical-member read targets missing data

- **WHEN** a caller directly requests a physical member whose index, archive, or physical member is missing
- **THEN** the cache reports the established file-not-found failure

#### Scenario: A direct physical-member read targets corrupt archive data

- **WHEN** a caller directly requests a physical member from corrupt archive data
- **THEN** the cache propagates the established container or archive decoding failure

### Requirement: Type providers decode each requested member independently

Providers MUST resolve requested children through the reference table and return factory fallbacks when the parent archive or child mapping is absent. For mapped entries, they MUST decode the mapped physical member into a fresh definition and propagate read or decode failures. Existing custom-provider delegation and post-decode behavior MUST remain intact.

#### Scenario: A provider decodes a present definition

- **WHEN** a generic or specialized provider reads a present definition
- **THEN** it decodes the mapped physical member into a fresh definition and invokes the existing post-decode behavior once

#### Scenario: The parent archive has no reference-table entry

- **WHEN** a provider requests a definition whose parent archive is absent from the reference table
- **THEN** it returns a factory fallback without attempting to read an archive file

#### Scenario: A child is absent from an existing archive

- **WHEN** a provider requests a definition whose parent archive exists but whose logical child has no reference-table entry
- **THEN** it returns a factory fallback without attempting to read an archive file

#### Scenario: A mapped archive file is missing

- **WHEN** a provider requests a mapped definition but its archive file is missing from storage
- **THEN** the cache's file-not-found failure propagates and the provider does not use the factory fallback

#### Scenario: A mapped archive is corrupt

- **WHEN** a provider requests a mapped definition but its archive data cannot be decoded
- **THEN** the cache's decoding failure propagates and the provider does not use the factory fallback
