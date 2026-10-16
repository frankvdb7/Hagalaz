# Design

## Context

See [proposal.md](proposal.md) for the current payout gap and scope. `DuelStakeExchange` already owns duel stake transfers and cancellation refunds. `DuelArenaCombatScript` currently performs victory payout as separate coin removal and inventory additions.

## Goals / Non-Goals

**Goals:**

- Give victory payout one atomic boundary over both stake containers and the winner's destination storage.
- Consume the source stakes as part of the payout so a repeated victory callback cannot award them again.
- Do not advance victory UI or trapdoor exit when payout is rejected; keep the stakes owned by the active combat script until a later attempt succeeds.

**Non-Goals:**

- Add a recovery destination or change duel staking and cancellation-refund behavior.
- Add a second settlement coordinator or a new transaction mechanism.

## Decisions

- Add victory settlement to the existing internal `DuelStakeExchange`. Keep `DuelArenaCombatScript`'s public constructor unchanged; the settlement operation does not need the exchange's item-builder state.
- Begin one `ItemContainerTransaction` with the two stake containers and the winner's MoneyPouch. MoneyPouch contributes the winner's inventory boundary.
- Snapshot each stake's occupied slots before moving entries. Transfer coins using the existing MoneyPouch coin-transfer operation and transfer other items through the winner's ordinary inventory transfer API. A rejected transfer exits without commit, so disposal restores every participant.
- Commit once after all entries transfer. Since stakes are consumed under the same transaction as destination credit, a later attempt sees no source items and cannot duplicate the payout.
- Settle before opening the victory screen. Capture the displayed stake contents before settlement and send that snapshot after success, because successful settlement empties the source containers.
- Make `StartVictoryStage` report whether the stage began. `Trapdoor` only teleports after success; on rejection it unlocks movement so the winner can free inventory space and try again. Capacity can change after duel confirmation because the Equipment screen allows an equipped item to be unequipped into Inventory during combat.

Alternatives considered: Passing the internal exchange through the combat script's public constructor would change that API; keeping payout logic in `DuelArenaCombatScript` would duplicate transfer policy; adding a settlement service or transaction API would introduce a second owner for behavior that already belongs to `DuelStakeExchange`.

## Risks / Trade-offs

- A full winner inventory rejects the whole payout and leaves the stakes unchanged in their existing containers. The active combat script retains those containers until settlement succeeds; this change does not add a new overflow destination.
- Transaction completion publication can throw after storage is irreversible. Because both stake sources have already been consumed, retrying the victory path cannot duplicate the payout.
