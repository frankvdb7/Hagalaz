## Context

`DuelArenaScript.StartDuel` creates two `DuelArenaCombatScript` instances with mirrored containers. Terminal entry points are the two `CreatureDestroyedEvent` handlers registered by each script, `OnKilledBy()` and the trapdoor; all of them go through `StartVictoryStage()`. `InMemoryEventBus.SendEvent` snapshots the handler list, so unregistering from the first callback does not stop the second one in the same dispatch.

## Decisions

- **One shared `DuelResult` per duel**, created in `StartDuel` and passed to both scripts through the constructor. It is the single owner of "has this duel been decided", replacing the implicit per-script `DuelSession` and the empty-escrow check as terminal guards. `DuelSession` still means "this participant is in an active duel" (busy state, rule handlers, hint icon).
- **Claim, then complete or release.** `TryClaim()` runs before settlement so re-entrant or repeated callbacks are rejected before side effects. `Complete()` follows a committed payout, a no-stakes result, or a typed post-commit exception. `Release()` in a `finally` makes a rejected or failed payout retryable and is a no-op once completed.
- **No lock.** Event dispatch, `OnKilledBy()` and trapdoor tasks run on the game thread; the claim only has to handle re-entrancy and ordering, not parallelism.
- **Trapdoor and end screens unchanged.** A completed result makes `StartVictoryStage()` return false; the trapdoor already treats false as "stay and unlock".

## Risks

- A trapdoor click after a completed result no longer teleports; the winner is already respawned and the loser is destroyed or respawned by the death continuation, so this state is not reachable in normal play.
