## MODIFIED Requirements

### Requirement: Generic and sprite reads require a parent archive

Generic `TypeProvider<T, TD>` and `SpriteTypeProvider` single-definition reads MUST fail with `FileNotFoundException` when the parent archive has no reference-table entry. Generic `TypeProvider<T, TD>.GetRange` MUST preserve the same missing-parent failure.

#### Scenario: A generic individual and range request target a missing parent archive

- **WHEN** `TypeProvider.Get` and `TypeProvider.GetRange` request a definition whose parent archive is absent from the reference table
- **THEN** both requests report `FileNotFoundException` and neither returns a factory fallback

#### Scenario: A sprite request targets a missing parent archive

- **WHEN** `SpriteTypeProvider.Get` requests a definition whose parent archive is absent from the reference table
- **THEN** it reports `FileNotFoundException` and does not return a factory fallback

### Requirement: Missing child mappings use provider fallbacks

Generic and sprite providers MUST return factory fallbacks when a parent archive exists but the requested child mapping is absent. Item, NPC, and object providers MUST retain factory fallbacks when either the parent archive or child mapping is absent.

#### Scenario: A child is absent from an existing archive

- **WHEN** a generic, sprite, item, NPC, or object provider requests a definition whose parent archive exists but whose child mapping is absent
- **THEN** it returns a factory fallback without reading a physical member

#### Scenario: A specialized provider's parent archive is absent

- **WHEN** an item, NPC, or object provider requests a definition whose parent archive is absent from the reference table
- **THEN** the provider returns its existing factory fallback

### Requirement: Mapped definitions decode through the cache API

Providers MUST decode mapped physical members through `ICacheAPI.Read` and propagate missing-file or decode failures. Existing custom-provider delegation and post-decode behavior MUST remain intact.

#### Scenario: A provider decodes a present definition

- **WHEN** a provider reads a present definition
- **THEN** it decodes the mapped physical member into a fresh definition and invokes existing post-decode behavior once

#### Scenario: A mapped archive file is missing or corrupt

- **WHEN** a provider requests a mapped definition but its archive file is missing or its data cannot be decoded
- **THEN** the cache's file-not-found or decoding failure propagates and the provider does not use a factory fallback
