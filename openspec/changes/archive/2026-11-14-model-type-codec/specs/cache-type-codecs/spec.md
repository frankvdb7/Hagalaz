# Spec Delta

## ADDED Requirements

### Requirement: Models re-encode byte for byte

Decoding a stored model and encoding the result MUST reproduce the stored bytes exactly for every model in the game cache, including texture mapping data on models without texture IDs and switches whose values are uniform.

#### Scenario: Round-tripping every stored model

- **WHEN** every model in the game cache is decoded and re-encoded
- **THEN** each encoding is byte-identical to the stored model

#### Scenario: Model with uniform alpha

- **WHEN** a model whose alpha switch is on stores the same alpha for every face
- **THEN** re-encoding keeps the alpha stream and the switch

### Requirement: Decoded models keep stored values

A decoded model MUST expose its stored values: absolute vertex positions, triangle indices with their strip codes, raw face colours, texture IDs as stored, raw mapping parameters, and the version and presence switches. Values used for rendering, such as version-12 projection scaling, texture IDs minus one and generated texture coordinates, MUST be derived from the stored values.

#### Scenario: Version-12 model

- **WHEN** a version-12 model is decoded
- **THEN** its positions are the stored values, and its texture projection uses positions and scales multiplied by four as before

### Requirement: Unsupported model layouts are rejected

Decoding MUST fail with a not-supported error for a model in the old layout or with extended skin tables, and MUST support billboard groups stored as two-byte values.

#### Scenario: Old-layout model

- **WHEN** a model without the newer layout's footer is decoded
- **THEN** decoding fails with a not-supported error

#### Scenario: Two-byte billboard groups

- **WHEN** a model with two-byte billboard groups is decoded and re-encoded
- **THEN** its billboard groups are preserved and the encoding is byte-identical

### Requirement: Model responses are unchanged

Cache API responses built from models MUST return the same values for the same cache as before models gained a codec. This covers model, item, NPC and object models, model and object images, and item icons.

#### Scenario: Textured model request

- **WHEN** a textured model with particle and billboard attachments is requested
- **THEN** its geometry, texture coordinates, first-texel faces, attachments and warnings match the previous response
