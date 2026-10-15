# trading — revision 742 research data

**Evidence tier:** period-preserved secondary reference, with pending client/cache verification.

## Period mechanics and security rules

The 1 February 2011 removal of direct trade value limits predates 742; **do not automatically implement restrictive earlier trade caps**. The trading process includes a request, offer screen, both-party acceptance, confirmation/review, transfer and cancellation. Source: [Exchange history](https://wiki.darkan.org/Grand_Exchange); exact trade confirmation widget IDs and UI text require capture.

**To implement:** one symmetric state machine with item/coin escrow belonging to each original owner until successful transfer. On offer edits, reset both accepts; confirmation shows quantities and values. Final validation must check both destination backpacks and pouches within a single transaction. Add disconnect and logout snapshot tests. Current Hagalaz capacity preview and coin reception are not fully congruent when pouch overflows; solve before calling parity complete.

**Data to capture:** tradable item flags, noted behavior, offer inventory/widget IDs, 742 client script/config updates, trade requests at distance, accept toggles, confirmation text, coin-pouch offer handling, anti-duplication race outcomes and rejection messages.

## Data to complete before full parity

The current source set does not provide exhaustive 742 item IDs, widget/config/packet IDs or an executable reference dataset. Capture those from the target client/cache and reconcile with Hagalaz's existing definitions before promoting candidate rules to constants.

[Implementation guide](../trading.md) · [Research policy](../research-evidence.md)
