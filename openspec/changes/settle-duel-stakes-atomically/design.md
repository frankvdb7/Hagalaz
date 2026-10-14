# Design

## Context

See [proposal.md](proposal.md) for the current payout gap and scope. `DuelStakeExchange` already owns duel stake transfers and cancellation refunds. `DuelArenaCombatScript` currently performs victory payout as separate coin removal and inventory additions.

## Goals / Non-Goals

**Goals:**

- Give victory payout one atomic boundary over both stake containers and the winner's destination storage.
- Consume the source stakes as part of the payout so a repeated victory callback cannot award them again.

**Non-Goals:**

- Change duel session, end-screen, or recovery behavior.
- Add a second settlement coordinator or a new transaction mechanism.

## Decisions

- Add victory settlement to the existing internal `DuelStakeExchange`. Keep `DuelArenaCombatScript`'s public constructor unchanged; the settlement operation does not need the exchange's item-builder state.
- Begin one `ItemContainerTransaction` with the two stake containers and the winner's MoneyPouch. MoneyPouch contributes the winner's inventory boundary.
- Snapshot each stake's occupied slots before moving entries. Transfer coins using the existing MoneyPouch coin-transfer operation and transfer other items through the winner's ordinary inventory transfer API. A rejected transfer exits without commit, so disposal restores every participant.
- Commit once after all entries transfer. Since stakes are consumed under the same transaction as destination credit, a later attempt sees no source items and cannot duplicate the payout.
- Keep the existing end-screen item display before settlement so the displayed stake contents retain their current timing.

Alternatives considered: Passing the internal exchange through the combat script's public constructor would change that API; keeping payout logic in `DuelArenaCombatScript` would duplicate transfer policy; adding a settlement service or transaction API would introduce a second owner for behavior that already belongs to `DuelStakeExchange`.

## Risks / Trade-offs

- A full winner inventory rejects the whole payout and leaves the stakes unchanged in their existing containers. This change does not add a new overflow destination.
- Transaction completion publication can throw after storage is irreversible. Because both stake sources have already been consumed, retrying the victory path cannot duplicate the payout.
