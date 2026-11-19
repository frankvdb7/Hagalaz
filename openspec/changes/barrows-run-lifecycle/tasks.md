## 1. Lifecycle

- [x] 1.1 Load the run from the constructor body; add real-construction and spawn tests.
- [x] 1.2 Keep unclaimed progress on exit; always send the exit message.
- [x] 1.3 Six-mound tunnel, persisted at creation; reset and new tunnel at chest search with a claim guard.
- [x] 1.4 Derive the brother count from flags; per-player kill credit; death keeps the run.
- [x] 1.5 Keep Akrisae locked.

## 2. Client encodings

- [x] 2.1 Config 453 packing, config 452 rope bits, rope 6712.
- [x] 2.2 Record confirmed 742 values in `docs/revision-742/reference/minigames.md`.

## 3. Rewards

- [x] 3.1 Move armour out of chest table 8 into per-brother tables 36-41 (manual database edit, no migration, no SQL in the repo); roll them for slain brothers in `BarrowsRewards`.

## 4. Centre-door puzzle

- [x] 4.1 `BarrowsPuzzle` (models, doors, reroll), `BarrowsPuzzleWidget` (interface 25), door handling in `BarrowCryptDoor`, run state in `BarrowsDto`.

## 5. Akrisae combat

- [x] 5.1 `AkrisaeProtection` and `AkrisaeTheDoomed`: melee attacks, protection after 150 damage from one style, reduced hits, skipped attack, reset on cancel and death.

## 6. Ahrim

- [x] 6.1 `AhrimTheBlighted.ApplyBlightedAura`: 20% chance of -5 Strength on a successful magic hit.

## 7. Reward potential

- [x] 7.1 `BarrowsRewards`: reward count, armour chance, potential and ordinary bands; crypt-creature levels tracked in `BarrowsDto.CreatureLevels`; `LootChest` rolls the chosen tables and stays unclaimed if one is missing.

## 8. Verification

- [x] 8.1 Run `Hagalaz.Game.Scripts.Tests` (569): all pass.
- [ ] 8.2 Remaining #315 work is tracked outside this change (quest persistence, ordinary reward scaling, live-client checks of the puzzle).
