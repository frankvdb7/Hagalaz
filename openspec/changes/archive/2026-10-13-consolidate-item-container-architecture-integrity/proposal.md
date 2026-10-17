# Proposal

## Why

Item-container synchronization still has separate multi-boundary acquisition loops, while standalone mutation ownership is not counted for its full lock lifetime. Equipment replacement still relies on compensating item recovery after sequential mutations, and the storage removal and shop normalization algorithms can change more item state than intended.

## What Changes

- Use one internal ordered lock-set implementation for transactions, standalone transfers, and ownership cleanup; account for standalone lock ownership throughout each operation.
- Replace Equipment's incoming-item compensation flow with aura-specific `EquipItem` interaction and an atomic inventory/equipment transaction.
- Make partial stack removal consume no more than the requested total quantity.
- Normalize shop stock from immutable item-ID baselines using a planned copy-on-write state.
- Avoid revision and publication changes for no-op sorting while preserving empty-clear and sentinel behavior.
- Audit concrete preflight reads and document the storage stacking callback contract.
- Update regression coverage and canonical item-container behavior requirements.

## Scope

### In scope

- `Hagalaz.Game.Abstractions` item-container synchronization, transaction, storage, and public item/equipment script contracts.
- GameWorld Equipment, Shop stock, inventory/drop, MoneyPouch, Bank, Rewards, and Familiar call paths only where required to preserve the stated invariants.
- Game Scripts aura confirmation and regression coverage for the affected scripts.
- The canonical `item-container-storage` OpenSpec capability.

### Non-goals

- No ambient transaction registry, async transaction support, global lock, generic callback/event framework, compensation manager, or new public transaction convenience API.
- No deep snapshot of arbitrary item metadata such as `ExtraData`.
- No redesign of trade/session state or MoneyPouch quantity semantics.
- No broad stacking-system redesign, global read lock, or unrelated generic container API cleanup.

## Acceptance Criteria

1. All multi-boundary acquisition uses one deterministic lock-set algorithm with reverse release, conflict-prefix release before waiting, nested nonblocking behavior, and ownership cleanup on failure.
2. Thread ownership accounting covers standalone mutation locks, retained standalone publication, explicit transactions, and multi-boundary standalone operations without gaps or leaks.
3. Transaction snapshots are captured only after every participant lock is acquired; a failed `Begin` leaves storage unchanged and no boundary bound.
4. Ordinary gameplay rejection continues to use existing bool/count results; lifecycle misuse and unexpected failures remain exceptions.
5. Storage stacking callbacks are documented as deterministic storage-level decisions and must not start transactions, mutate unrelated containers, send events, or block on unrelated resources.
6. Active aura confirmation is handled by `AuraEquipmentScript.EquipItem` without holding container locks; acceptance delegates to the existing atomic Equipment operation, which revalidates exact item instances.
7. Equipment replacement has no Rewards/Bank recovery fallback and never compensates after commit.
8. `Remove` removes at most its requested total quantity; exact removal stays all-or-nothing; inventory drop creates no ground item after failed exact removal.
9. Shop normalization uses immutable aggregated original quantities and copy-on-write planning; failed planning leaves storage, revisions, and unchanged item identities intact.
10. No-op sort and empty clear do not advance revision or publish; a zero-count sentinel remains occupied.
11. Decision-critical preflight reads are validated again under all relevant mutation ownership where a concrete unsafe path is found.
12. Canonical OpenSpec describes the resulting behavior and contains no obsolete recovery-based Equipment requirement.
13. Focused regression suites, full solution build/tests, strict OpenSpec validation, duplication gate, three read-only reviews, and `git diff --check` complete successfully.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `item-container-storage`: synchronization ownership, Equipment replacement, removal totals, shop normalization, revision/publication behavior, and decision-critical mutation checks.

## Impact

Changes are limited to existing .NET abstractions, GameWorld domain containers, Game Scripts equipment behavior, their tests, and OpenSpec. No dependency or remote PR configuration change is planned.
