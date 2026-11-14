# Spec Delta

## ADDED Requirements

### Requirement: Animation skeletons and frames re-encode byte for byte

Decoding a stored animation skeleton or frame and encoding the result MUST reproduce the stored bytes exactly for every entry in the game cache. This includes flag bytes whose axis values are zero and frames that store fewer slots than their skeleton.

#### Scenario: Round-tripping every stored skeleton and frame

- **WHEN** every skeleton and every frame in the game cache is decoded and re-encoded
- **THEN** each encoding is byte-identical to the stored entry

#### Scenario: Slot with tween flags only

- **WHEN** a frame slot stores tween flags without any axis bit
- **THEN** re-encoding keeps that flag byte

### Requirement: Frames decode without their skeleton

A stored frame MUST be decodable and re-encodable on its own. Its skeleton MUST only be needed to derive the transforms consumers see.

#### Scenario: Decoding a frame alone

- **WHEN** a frame is decoded without loading the skeleton it references
- **THEN** its header, skeleton ID, slot flags and stored values are available

### Requirement: Animation frame responses are unchanged

The animation frame response MUST return the same transforms, skeleton types, enabled flags, masks and groups for the same cache as before skeletons and frames gained codecs. A frame with more slots than its skeleton, and truncated or trailing data, MUST still be rejected.

#### Scenario: Requesting a frame

- **WHEN** an animation frame is requested
- **THEN** its transforms, implied pivots, tween flags and skeleton fields match the previous response

#### Scenario: Frame larger than its skeleton

- **WHEN** a requested frame stores more slots than its skeleton has
- **THEN** the request fails as an unreadable frame
