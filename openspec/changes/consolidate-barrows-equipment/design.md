## Decision

One abstract `BarrowsEquipment` is the single owner of the shared rules. A concrete set supplies `ItemIds` (20 IDs ordered hat, weapon, chest, legs; each part is its original ID followed by four degraded IDs) and its own state type.

- Set detection: all of hat, weapon, chest and legs must be equipped and each ID must be in the set's 20 IDs (any stage).
- Ticks: 0 for an original ID (index % 5 == 0), 22,500 otherwise; death factor 0.80.
- Successor: an original ID maps to the next ID declared in its part; every other ID maps to `id + 1`. The first successor comes from the declared table, never from numeric spacing (spacing differs per set, Akrisae is +2).

State transitions stay in the concrete scripts (`OnEquipped`/`OnUnequipped`), so the typed state remains visibly tied to its set. `GuthansWarSpear`/`KarilsCrossbow` still inherit the set behavior and register their own five weapon IDs, so one handler resolves per item ID.

## Rejected alternatives

- Separate explicit first-successor arrays: identical to "the next entry in the declared table" for all 28 original IDs, so a second table only adds a place to diverge.
- Generic state factories, rule registries or injected constructors: no second consumer, more indirection than seven two-line hooks.
- Adding weapon IDs to the Guthan/Karil attributes: would create competing registrations.
- A separate internal helper in addition to the base class: the issue asks for one owner only.
