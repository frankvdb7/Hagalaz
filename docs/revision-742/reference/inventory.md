# inventory — revision 742 research data

**Evidence tier:** period-preserved secondary reference, with pending client/cache verification.

## Core inventory and item behavior

- Normal backpack capacity: **28 slots**.
- Item stacks depend on item definition; a bank may stack item types that occupy separate backpack slots. Notes are separate item definitions with unnoted mappings.
- Interaction categories include item-on-item, item-on-NPC, item-on-object, equipment, use, destroy/drop, examine and moving items between container widgets.
- Items should carry precise instance attributes (charges, durability, cosmetic customization) when present; reference data alone cannot establish these flags.

Sources: [Bank stacking behavior](https://wiki.darkan.org/Bank), [tool-belt interactions](https://wiki.darkan.org/Toolbelt).

**Exact extract still needed:** item database for 742 (ID/name, stack behavior, note ID, equipment slot, tradeability, death treatment, alchemy values, wear requirements, scripted options and widget mapping). This is a cache/codecs + Hagalaz definition reconciliation exercise, not a manual list of every item. Use authoritative item-container mutation boundary for moves.

## Data to complete before full parity

The current source set does not provide exhaustive 742 item IDs, widget/config/packet IDs or an executable reference dataset. Capture those from the target client/cache and reconcile with Hagalaz's existing definitions before promoting candidate rules to constants.

[Implementation guide](../inventory.md) · [Research policy](../research-evidence.md)
