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
Normal loads MUST remain owned by MapRegionLoadScheduler and MapRegionLoader. Dynamic destinations publish as exact canonical Initializing regions and bypass that loader. A standard source maps every part to itself, so it MUST NOT be created, loaded or awaited: only its map template is read, once. A dynamic source MUST NOT be given to the normal load scheduler: it is awaited through the task that populates it, resumed if it was suspended, and revalidated as the exact canonical active Ready instance before its part mappings are read. Copy runs on serialized GameWorker; the destination becomes Ready only after success.

#### Scenario: A fresh dynamic destination is created

- **WHEN** dynamic creation requests a destination with no existing region
- **THEN** the service MUST publish one dynamic `Initializing` destination
- **AND** it MUST NOT request normal loading for that destination
- **AND** the destination MUST remain not-ready until GameWorker copy completes

#### Scenario: A standard source is not loaded

- **WHEN** dynamic creation uses a standard source region
- **THEN** the service MUST NOT create, load or await the source region
- **AND** the source's template MUST be read once and every part MUST map to the same part of the source, unrotated

#### Scenario: Dynamic creation waits for its source

- **WHEN** the source is a dynamic region that is still `Initializing` (a standard source is never awaited)
- **THEN** dynamic creation MUST wait for the task that populates it and MUST NOT request normal loading for it
- **AND** it MUST NOT read the source's part mappings before the exact source is canonical and `Ready`
- **AND** an idle (suspended) dynamic source MUST be resumed, not rejected

#### Scenario: Dynamic population is canceled or fails

- **WHEN** dynamic population is canceled or fails before readiness
- **THEN** the exact destination MUST be discarded and exact-removed on the
  GameWorker boundary
- **AND** a newer replacement destination MUST remain untouched
- **AND** the destination MUST NOT publish `Ready`

#### Scenario: Readiness waits for a dynamic destination's copy

- **WHEN** any caller (for example sign-in) requires an `Initializing` dynamic region to be loaded
- **THEN** the normal load scheduler MUST NOT invoke the normal loader for it
- **AND** the caller MUST wait for the dynamic copy to finish, and MUST fail if the copy fails
