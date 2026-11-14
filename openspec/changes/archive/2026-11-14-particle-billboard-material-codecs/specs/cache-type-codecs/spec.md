# Spec Delta

## Purpose

Lets every cache definition type be decoded from and encoded back to its stored bytes without loss, so definitions can be reused across the cache library and written back safely.

## ADDED Requirements

### Requirement: Definition types preserve every stored value

Decoding and re-encoding a stored particle emitter, particle effector, model billboard, render animation set or material table entry MUST preserve every stored value, including opcodes and columns that no current consumer uses. Encoding MUST be byte-stable, and the material table MUST re-encode byte for byte.

#### Scenario: Round-tripping every stored entry

- **WHEN** every stored entry of these five definitions in the game cache is decoded, encoded and decoded again
- **THEN** the second decoding has the same values as the first, and encoding it again gives identical bytes

#### Scenario: Round-tripping the material table

- **WHEN** the stored material table is decoded and re-encoded
- **THEN** the encoding is byte-identical to the stored table

#### Scenario: Entry with a repeated opcode

- **WHEN** a render animation set entry stores the same opcode twice with different values
- **THEN** the decoded definition holds the last value, as the client does

#### Scenario: Entry with an opcode no consumer reads

- **WHEN** a particle emitter whose stored entry contains opcodes the model viewer ignores is decoded and re-encoded
- **THEN** those opcodes and their payloads are preserved

### Requirement: Decoded definitions keep stored values

A decoded definition MUST expose its stored values unscaled and unremapped. Values shown to consumers in another unit or with a substituted default MUST be derived from the stored values, not stored in their place.

#### Scenario: Billboard with no texture

- **WHEN** a billboard entry stores texture ID 65535
- **THEN** the decoded definition keeps the stored value, and consumers still see texture ID -1

#### Scenario: Effector with zero attenuation

- **WHEN** a particle effector entry stores an attenuation of 0
- **THEN** re-encoding writes 0, and consumers still see an attenuation of 1

### Requirement: Undecodable entries are rejected

Decoding MUST fail for an entry that is truncated, lacks its terminator, or contains an opcode whose payload layout is unknown, rather than returning a partial definition.

#### Scenario: Truncated emitter

- **WHEN** a particle emitter entry ends in the middle of an opcode payload
- **THEN** decoding fails with an invalid-data error

#### Scenario: Unknown billboard opcode

- **WHEN** a billboard entry contains an opcode with no known payload layout
- **THEN** decoding fails and no definition is returned

### Requirement: Consumers see unchanged definition data

Cache API responses that expose these definitions MUST return the same values for the same cache as before the types gained codecs. This covers model particles, billboards and effectors, NPC render-animation sequences, material and texture images, and item icons.

#### Scenario: Model with particles

- **WHEN** a model with particle emitters and effectors is requested
- **THEN** every emitter and effector value and availability warning matches the previous response, and the unsupported-opcode list holds the same opcodes in ascending order without repeats

#### Scenario: NPC with a render animation set

- **WHEN** an NPC model whose render animation set stores op 1 after other sequence opcodes is requested
- **THEN** the same sequence IDs are returned, with the NPC's own sequences first, then op 1's stand and walk sequences, then the others in ascending opcode order

#### Scenario: Material request

- **WHEN** a texture's material is requested
- **THEN** every material field matches the previous response
