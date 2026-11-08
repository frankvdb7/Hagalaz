# Model material effects

## Purpose

Allow reusable model inspection to preview source-selected animated materials while distinguishing verified behavior from approximations and unavailable scene effects.

## ADDED Requirements

### Requirement: Materials scroll at source-defined rates
The viewer SHALL apply signed U and V scroll values at rawByte / 64 texture periods per second using explicit elapsed time.

#### Scenario: Signed scrolling
- **GIVEN** a material with U scroll -32 and V scroll 16
- **WHEN** elapsed time is two seconds
- **THEN** its texture coordinates have advanced -1 U period and 0.5 V periods with unchanged wrap modes

### Requirement: Effect identity controls material previews
The viewer SHALL select advanced previews from source effect IDs and parameters, covering IDs 1, 2, 4, 5, 6, 7, 8 and 9. It SHALL label approximations and capability fallbacks and preserve original identity.

#### Scenario: Supported effect
- **GIVEN** an available supported effect material
- **WHEN** a model using it is selected
- **THEN** the corresponding preview uses its decoded parameters and displays its fidelity classification

#### Scenario: Unresolved effect
- **WHEN** a material uses category 3 or an unknown effect
- **THEN** a base-material fallback is displayed with the source ID and an unsupported message

#### Scenario: Absent scene inputs
- **WHEN** an effect requires unavailable terrain, shoreline or environment inputs
- **THEN** the preview identifies its fallback without claiming scene-effect parity

### Requirement: HDR material data retains source semantics
Protected rendering assets SHALL expose supported HDR companion data without discarding intensities above one or changing existing PNG routes.

#### Scenario: HDR multiplier
- **GIVEN** a companion sample h and an RGB source value c normalized to zero through one
- **WHEN** HDR appearance is decoded
- **THEN** the resulting color is c multiplied by 1 + h * 31 / 4096 according to the client sample convention

#### Scenario: Missing companion
- **WHEN** required companion data is unavailable
- **THEN** the viewer reports the limitation and uses an identified base-image fallback

#### Scenario: Unauthorized asset request
- **WHEN** an unauthorized caller requests effect rendering assets
- **THEN** the existing cache authorization boundary denies access

### Requirement: Effect resources follow viewer lifetime
Replacing or destroying a preview SHALL release its effect resources, cancel obsolete asset work and prevent stale results from affecting the next selection.

#### Scenario: Replacement during loading
- **WHEN** a model is replaced while effect data is loading
- **THEN** obsolete data is discarded and its resources are released

#### Scenario: Retry
- **WHEN** an administrator retries a failed effect asset request
- **THEN** successful data replaces the identified fallback without creating another animation loop
