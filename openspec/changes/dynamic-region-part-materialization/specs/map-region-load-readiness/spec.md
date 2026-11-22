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
