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

### Requirement: Armour rewards come only from slain brothers

A chest claim SHALL roll armour only from the per-brother tables of brothers slain in the run, with `min(N+1, 7)` rolls each hitting at 1 in `max(450 - 58N, 73)`, and SHALL NOT roll armour when no brother was slain. The ordinary chest table SHALL contain no armour.

#### Scenario: One brother slain

- **WHEN** only Karil was slain
- **THEN** only Karil's armour table can be rolled

### Requirement: One chest-room door is operable and carries the puzzle

Each run SHALL have exactly one operable door among the four doors around the chest; config 452 SHALL lock the other three until the puzzle is solved. The operable door SHALL open interface 25 with one of four shape puzzles, three sequence models and three shuffled choices, one correct. A correct answer SHALL let the player through and unlock the doors for the rest of the run; a wrong answer SHALL move the operable door to a different door and pick a puzzle.

#### Scenario: Wrong answer

- **WHEN** the player picks a wrong shape
- **THEN** a different chest-room door becomes operable, the locked-door bits are resent, and the player does not pass

#### Scenario: Correct answer

- **WHEN** the player picks the correct shape
- **THEN** the player passes, the lock bits clear, and the next claim starts a fresh puzzle
