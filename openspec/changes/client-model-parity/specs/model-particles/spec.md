## MODIFIED Requirements

### Requirement: Model assets retain particle attachments
Protected model data SHALL retain emitter definitions and attachment faces through raw decoding and definition assembly, with visible warnings for unavailable definitions.

#### Scenario: Equipped completionist cape
- **WHEN** item20769's equipped model is requested
- **THEN** its seven emitter403 face attachments are retained

#### Scenario: Inventory cape
- **WHEN** item20769's inventory model is requested
- **THEN** no equipped-only emitter is fabricated

#### Scenario: Attached effector
- **WHEN** a model containing a supported effector is decoded and assembled
- **THEN** the effector retains its definition and references the correct attachment in the assembled geometry

### Requirement: Particle previews follow model lifetime
The shared viewer SHALL animate source-textured particles from current attachment geometry using bounded resources and release them on replacement or destruction.

#### Scenario: Posed attachment
- **WHEN** an attached face moves in an animation
- **THEN** newly emitted particles originate on that face's current position

#### Scenario: Replacement
- **WHEN** a model is replaced or the viewer is destroyed
- **THEN** its particles stop and their rendering resources are released

#### Scenario: Animated effector
- **WHEN** a supported effector moves with an animated model
- **THEN** its force is evaluated using the current posed attachment and the client definition

### Requirement: Particle fidelity limitations are visible
The viewer SHALL identify missing particle assets and unsupported or unverified simulation behavior without claiming full client parity.

#### Scenario: Unsupported behavior
- **WHEN** an emitter uses unavailable force or advanced lifecycle behavior
- **THEN** the preview displays its limitation and retains supported settings

#### Scenario: Partially supported emitter definition
- **WHEN** a definition mixes verified emitter settings with settings whose behavior is still unverified
- **THEN** the viewer applies the verified settings and identifies the remaining unsupported opcodes
