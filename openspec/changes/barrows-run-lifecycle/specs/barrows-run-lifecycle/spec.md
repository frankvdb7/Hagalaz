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

A chest claim SHALL give `min(N+1, 7)` rewards for N slain brothers. Each reward SHALL be a slain brother's armour table with a 1 in `max(450 - 58N, 73)` chance, and SHALL NOT be armour when no brother was slain.

#### Scenario: One brother slain

- **WHEN** only Karil was slain
- **THEN** only Karil's armour table can be rolled

### Requirement: One chest-room door is operable and carries the puzzle

Each run SHALL have exactly one operable door among the four doors around the chest, and config 452 SHALL lock the other three until the puzzle is solved.

#### Scenario: Locked doors

- **WHEN** the character is in the crypts and the puzzle is unsolved
- **THEN** config 452 locks the three non-operable chest-room doors

### Requirement: The puzzle door asks which shape comes next

The operable door SHALL open interface 25 with one of four shape puzzles: three sequence models and three shuffled choices, one correct. A correct answer SHALL let the player through and unlock the doors for the rest of the run; a wrong answer SHALL move the operable door, move the exit rope to a different entry, and pick a new puzzle.

#### Scenario: Wrong answer

- **WHEN** the player picks a wrong shape
- **THEN** a different chest-room door becomes operable, the rope moves to a different entry, the configuration is resent, and the player does not pass

#### Scenario: Correct answer

- **WHEN** the player picks the correct shape
- **THEN** the player passes, the lock bits clear, and the next claim starts a fresh puzzle

### Requirement: Akrisae protects against the style that hurts him

Akrisae SHALL attack with melee. After 150 damage from one combat style he SHALL protect against that style, reduce its hits to 60%, and skip his next attack. The protection SHALL reset when he loses his target or dies.

#### Scenario: Switching protection

- **WHEN** magic hits have dealt 150 damage to Akrisae
- **THEN** magic hits are reduced, other styles are not, and his next attack is skipped

#### Scenario: Misses

- **WHEN** a hit misses
- **THEN** it does not count towards a switch

### Requirement: Ahrim's Blighted Aura lowers Strength

A successful magic hit by Ahrim SHALL lower the target's Strength by 5 levels with a 20% chance.

#### Scenario: Aura proc

- **WHEN** a successful hit rolls under the 20% chance
- **THEN** the target's Strength is lowered by 5 levels

#### Scenario: No proc

- **WHEN** the roll is 20% or higher, or the hit misses
- **THEN** Strength is unchanged

### Requirement: Ordinary rewards scale with the creatures slain

Every other reward SHALL be chosen by a random roll in [0, potential], where potential is the summed combat levels of crypt creatures slain this run (capped at 1,000) plus 2 per slain brother. The roll SHALL select coins (0-379), mind (380-504), chaos (505-629), death (630-754), blood (755-879), bolt racks (880-1004), a loop half (1005-1007), a tooth half (1008-1010) or a dragon medium helm (1011 and above). A claim SHALL NOT complete if a selected loot table is missing.

#### Scenario: No creatures slain

- **WHEN** no crypt creature and no brother was slain
- **THEN** the single reward is a coins roll

#### Scenario: Crypt creature slain

- **WHEN** a giant crypt spider (combat level 79) is slain
- **THEN** 79 is added to the reward potential and persisted
