## Context

`TradingCharacterScript` is the owner of the current trade state and its two existing `TradeContainer` instances. Component callbacks are invoked directly by hub handlers, so callbacks from the two connections can run concurrently even though the normal game tick is serialized. Completion currently checks capacity and then performs several unchecked inventory and money-pouch mutations before calling cancellation as cleanup. The target character's own script is not currently linked to the owning trade, so target destruction cannot reliably enter the same cleanup path.

## Goals / Non-Goals

**Goals:**

- Give one active trade one serialized operation boundary covering acceptance, offer changes, completion, cancellation, and linked target cleanup.
- Make terminal state transitions explicit and owned by the initiating `TradingCharacterScript`.
- Preflight both recipients, apply the exchange using existing item-container and money-pouch APIs, and compensate only the exact trade deltas if a checked mutation fails.
- Keep escrow in the existing trade containers until the complete exchange or refund succeeds.
- Make disconnect cleanup deterministic by storing unrefundable escrow in the existing persisted rewards container, with the bank as an existing-capacity fallback.
- Preserve the existing character persistence behavior after in-memory character state changes.

**Non-Goals:**

- A durable trade ledger, database transaction, or distributed lock.
- A general inventory transaction framework or changes to unrelated container users.
- A new hosted service, queue, or trade service.
- Changes to distributed character-save ordering covered by issue #346.

## Decisions

1. **Use one session gate and terminal state owned by the initiator.** A small session state object contains the gate, target identity, and active/completing/completed/cancelled state. Final acceptance enters `completing` while holding the gate; only the active state can be cancelled or modified. Repeated final accepts and late callbacks observe a terminal/non-active state and return. A BCL `lock` is sufficient because the protected operations are synchronous and do not await.

   A game-thread queue was rejected because current component handlers do not dispatch through it, and routing only one side through a character queue would create a second ordering boundary for the same trade. A generic transaction coordinator was rejected because this is one in-memory domain operation with no durable transaction participant.

2. **Link the target lifecycle to the owner session.** When the owner starts a trade, the target's existing `TradingCharacterScript` receives a reference to the same session state. Target interruption/destruction forwards cancellation to the owner, and terminal cleanup removes the link. The target script does not become a second state owner and cannot complete the trade independently.

3. **Make all trade mutations checked and serialized.** Existing offer handlers will use focused script methods that hold the session gate while moving an item or coin between a character container and a trade container. If the destination trade-container add or refund fails, the source mutation is reversed and the operation remains non-terminal. Acceptance records the offer-container revisions; completion verifies both revisions and both accepted flags before using the offers.

4. **Apply the exchange as a checked two-recipient operation.** The owner separates coin offers from item offers, validates item capacity and money-pouch overflow capacity for each recipient, and checks every `AddRange`/money-pouch result. The public trade-container contract exposes only checked success/failure; it does not publish a reusable reversible mutation object. The opt-in trade-container implementation keeps the exact slot/item delta internal to the checked operation and rolls back a failed single-container mutation at that boundary, preserving unrelated changes to the same stack when the mutation remains identifiable; it never reconstructs a mutation from an aggregate count or replaces a whole container. The exchange coordinator may retain that internal trade-only receipt across the two-recipient operation when a later participant fails, but unrelated containers keep their existing public behavior. Storage mutation success is committed before observer delivery; notification success is recorded independently and never causes compensation. Rollback notifications are best-effort observer delivery and cannot turn an already-restored data mutation back into a transaction failure. The money-pouch abstraction owns its pouch/inventory overflow behavior and uses the same internal trade-only checked deltas. The exchange reports compensation-incomplete separately only when the exact mutation boundary cannot identify a safe inverse, so completion cannot fall through to refund while a recipient still holds an unremoved mutation. The offer containers are cleared only after both recipients receive their complete opposite offer, and escrow remains available for one consistent cancellation/refund attempt.

   A remove-first exchange was rejected because it creates a period in which a later destination failure requires reconstructing escrow. A destination-only preflight without compensation was rejected because capacity can change between validation and mutation and existing mutation APIs report failure. Whole-container replacement was rejected because unrelated gameplay could mutate a character container outside the trade gate.

5. **Keep one terminal cleanup implementation.** Successful completion transitions to `completed` and invokes shared interface/input/link reset without refund. Ordinary cancellation transitions only after all escrow is returned successfully; it invokes the same reset method with refund already complete. Destruction first attempts the normal refund and, when untouched cancellation escrow cannot fit, synchronously stores that escrow in the participant's persisted rewards container or bank before terminal cleanup. If exchange compensation is already pending, destruction retries only the exact recipient compensation and does not use Rewards/Bank to settle a partial exchange or refund its remaining escrow. This keeps persistence as a last-resort destination for cancellation escrow without making it a third participant in the exchange transaction, and prevents a successful exchange from reaching the old refund branch.

## Risks / Trade-offs

- [Risk] A non-standard trade container implementation could report a checked mutation failure after a partial update → the opt-in trade implementation keeps the exact applied items and slot identity private to the checked trade operation, applies the inverse delta when the trade change remains identifiable, and otherwise leaves the exchange compensation-pending; persistence is used only for untouched cancellation escrow, never to settle a partial exchange.
- [Risk] An observer notification can fail after storage mutation → the checked result records notification failure separately, while the committed data remains authoritative and the trade does not compensate it as if the mutation failed.
- [Risk] Holding a synchronous lock while container update events notify listeners could expose re-entrant callbacks → the gate is re-entrant for the owning thread, terminal state is set before cleanup callbacks, and no asynchronous wait occurs while held.
- [Risk] A refund may be temporarily impossible if a character's inventory changed after offering → ordinary cancellation remains pending for a linked retry, while destruction uses the existing persisted rewards/bank containers before clearing in-memory escrow.

## Migration Plan

No data migration or deployment sequencing is required. Deploy the script and focused regression tests together. Rollback is a code rollback; no persisted trade-specific state is introduced.

## Open Questions

None for this scoped fix. Distributed persistence ordering remains explicitly deferred to issue #346.
