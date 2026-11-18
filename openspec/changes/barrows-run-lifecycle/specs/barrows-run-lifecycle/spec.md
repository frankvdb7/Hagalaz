## ADDED Requirements

### Requirement: Run state is restored from the profile

The Barrows script SHALL restore killed flags, kill count, tunnel mound, entry quadrant and claim flag from the `minigames.barrows` profile object when it is created, using its injected dependencies, and SHALL persist a newly randomized tunnel immediately.

#### Scenario: Re-entry

- **WHEN** a character with stored kills enters the Barrows
- **THEN** those brothers are marked killed and the HUD and prayer drain reflect them

#### Scenario: First visit

- **WHEN** no profile object exists
- **THEN** a tunnel mound in 0..5 is chosen and stored

### Requirement: Unclaimed progress survives leaving

Leaving the Barrows SHALL NOT clear unclaimed progress and SHALL always send "The power of all those you slew in the barrows drains from your body.". Living brothers are despawned.

#### Scenario: Exit before claim

- **WHEN** the character leaves without claiming
- **THEN** the profile is unchanged and the message is sent

### Requirement: Searching the chest claims once and resets the run

Searching the chest SHALL grant the loot once, clear killed flags and kill count, choose a new tunnel mound, and keep the claim flag until the character leaves, after which the next run starts fresh. A missing loot table SHALL NOT mark the run claimed. A claimed chest SHALL NOT spawn the tunnel brother.

#### Scenario: Second search

- **WHEN** a claimed chest is searched again
- **THEN** no loot is generated

### Requirement: Kill credit and death

A brother kill SHALL count only for the NPC spawned for that player. On death the run SHALL be preserved and living spawned brothers despawned; an already-dead brother SHALL be left so its delayed kill credit is awarded.

#### Scenario: Death during a fight

- **WHEN** the character dies while a brother is alive
- **THEN** the brother despawns and the kill flags are unchanged

### Requirement: Akrisae stays locked

The Akrisae encounter SHALL NOT spawn until a persisted Ritual of the Mahjarrat completion source is available to scripts.

#### Scenario: Sarcophagus

- **WHEN** the Akrisae sarcophagus is searched
- **THEN** "You found nothing." is shown and no NPC is built

### Requirement: Client configuration encodings match the 742 cache

Config 453 SHALL carry brother flags in bits 0-5, chest open in bit 16 and kill count from bit 17 (no Akrisae flag). Config 452 SHALL be `1 << (6 + entryIndex)` for the four entries. Ropes 6709-6712 SHALL be handled.

#### Scenario: Entry quadrants

- **WHEN** the character enters the crypts through entry 0, 1, 2 or 3
- **THEN** config 452 is 64, 128, 256 or 512
