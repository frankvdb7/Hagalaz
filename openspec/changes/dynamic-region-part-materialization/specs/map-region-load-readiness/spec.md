# Spec Delta

## MODIFIED Requirements

### Requirement: Dynamic regions preserve source and destination dimensions

When a dynamic region copies a mapped part, the system MUST resolve source
objects and source floor collision from the source region's map template,
which does not depend on a dimension. Any copied object MUST be created with
the destination region's dimension, and the destination region MUST retain
its own dimension identity. Mapped state MUST be represented explicitly; hash
codes MUST NOT determine whether a part has a source mapping.

#### Scenario: A dimension-zero source is copied into dimension one

- **WHEN** a dynamic region in dimension 1 maps a part from a source region in
  dimension 0
- **THEN** source content MUST come from the source region's map template
- **AND** no live source region in any dimension MUST be read for objects or
  collision
- **AND** copied object locations MUST use dimension 1

#### Scenario: An erased dynamic part is empty

- **WHEN** a dynamic part is erased
- **THEN** the part MUST be explicitly unmapped
- **AND** loading that part MUST not perform template lookup or copy objects

### Requirement: Dynamic destinations have a separate readiness path
Normal loads MUST remain owned by MapRegionLoadScheduler and MapRegionLoader. A dynamic destination is never visible while incomplete: its id is only reserved while its source mappings and templates load, and it is registered, populated and marked `Ready` within one game task without an await in between. A standard source maps every part to itself, so it MUST NOT be created, loaded or awaited: only its map template is read, once. A dynamic source MUST NOT be given to the normal load scheduler: it is resumed if it was suspended and revalidated as the exact canonical active Ready instance before its part mappings are used.

#### Scenario: A fresh dynamic destination is created

- **WHEN** dynamic creation requests a destination with no existing region
- **THEN** the service MUST reserve the destination id and expose no region under it while templates load
- **AND** it MUST NOT request normal loading for that destination
- **AND** it MUST publish the destination as a `Ready` dynamic region only after every part is populated

#### Scenario: A reserved destination is requested before it is ready

- **WHEN** any caller requests the reserved destination id before the dynamic region is published
- **THEN** the service MUST fail the request explicitly and MUST NOT create a standard region in its place

#### Scenario: A standard source is not loaded

- **WHEN** dynamic creation uses a standard source region
- **THEN** the service MUST NOT create, load or await the source region
- **AND** the source's template MUST be read once and every part MUST map to the same part of the source, unrotated

#### Scenario: Dynamic creation waits for its source

- **WHEN** the source is a dynamic region (it is only ever visible once `Ready`, so there is nothing to wait for)
- **THEN** it MUST NOT be requested from the normal load scheduler
- **AND** it MUST be the exact canonical `Ready` instance before its part mappings are used
- **AND** an idle (suspended) dynamic source MUST be resumed, not rejected

#### Scenario: Dynamic population is canceled or fails

- **WHEN** dynamic creation is canceled or fails before the destination is published
- **THEN** no destination region MUST ever have been visible and the reservation MUST be released
- **AND** when population fails after publication the exact destination MUST be discarded and exact-removed
- **AND** the destination MUST NOT publish `Ready`
