## 1. Lifecycle

- [x] 1.1 Load the run from the constructor body; add real-construction and spawn tests.
- [x] 1.2 Keep unclaimed progress on exit; always send the exit message.
- [x] 1.3 Six-mound tunnel, persisted at creation; reset and new tunnel at chest search with a claim guard.
- [x] 1.4 Derive the brother count from flags; per-player kill credit; death keeps the run.
- [x] 1.5 Keep Akrisae locked.

## 2. Client encodings

- [x] 2.1 Config 453 packing, config 452 rope bits, rope 6712.
- [x] 2.2 Record confirmed 742 values in `docs/revision-742/reference/minigames.md`.

## 3. Verification

- [x] 3.1 Run `Hagalaz.Game.Scripts.Tests` (502): all pass.
- [ ] 3.2 Remaining #315 work is tracked outside this change (puzzle, chest rows, NPC specials, quest persistence).
