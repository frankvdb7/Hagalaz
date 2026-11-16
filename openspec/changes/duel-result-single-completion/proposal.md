## Why

An active duel installs two `DuelArenaCombatScript` instances (one per participant), each with its own `DuelSession` flag. A participant's destruction dispatches a `CreatureDestroyedEvent` to the handlers of both scripts, and the event bus snapshots them, so both can enter `StartVictoryStage()`. The first call settles stakes, respawns the winner and opens the end screens; the second still sees an active instance-local session and repeats winner respawn and victory UI, with empty spoils (issue #518). Non-staking duels have no escrow to act as a guard at all. The stake transfer itself is already atomic (#516) and is not part of this change.

## What Changes

- Add a small duel-owned `DuelResult` created once when combat begins and passed to both combat scripts.
- `StartVictoryStage()` claims the result before any side effect and rejects the call if it is already claimed or completed.
- The claim is completed once payout is committed (or immediately for a no-stakes duel, or after a typed post-commit failure) and released when `DuelStakeExchange.TrySettleWinner()` rejects the payout so it can be retried.
- Destruction handlers, `OnKilledBy()` and the trapdoor all reach the result through `StartVictoryStage()`.

### Non-goals

- No change to `DuelStakeExchange`, item containers, payout policy (#519), winner/loser selection, forfeit rules or end-screen content.
- No lock, state machine, retry worker or persistence: duel callbacks run on the game thread, so a plain claim flag is enough.

## Capabilities

### New Capabilities

- `duel-arena-result`: a duel completes its result exactly once across both participants.

## Impact

- `Hagalaz.Game.Scripts/Minigames/DuelArena/DuelResult.cs` (new), `DuelArenaCombatScript.cs`, `DuelArenaScript.cs`.
- `Hagalaz.Game.Scripts.Tests/Minigames/DuelArena/`.
