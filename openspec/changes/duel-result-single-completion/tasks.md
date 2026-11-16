## 1. Implementation

- [x] 1.1 Add `DuelResult` (claim / complete / release).
- [x] 1.2 Create one `DuelResult` in `DuelArenaScript.StartDuel` and pass it to both combat scripts.
- [x] 1.3 Claim in `StartVictoryStage()` before side effects; complete after payout; release on rejection or failure.

## 2. Tests

- [x] 2.1 Real `InMemoryEventBus` with both scripts: destroy callbacks in survivor-first and loser-first order complete once.
- [x] 2.2 Repeated victory stage, `OnKilledBy()`, destruction and forfeit do not repeat terminal effects.
- [x] 2.3 Update the existing repeated-`OnKilledBy` assertion; existing retry-after-capacity and post-commit tests stay green.

## 3. Verification

- [x] 3.1 Duel Arena and Game Scripts tests, solution build, `openspec validate --all --strict`, `git diff --check`.
