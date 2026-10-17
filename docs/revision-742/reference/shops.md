# shops — revision 742 research data

**Evidence tier:** period-preserved secondary reference, with pending client/cache verification.

## NPC shop semantics

Period NPC shop content uses separate general/specialized store stocks, items offered for purchase and local buy-back rules. Pricing is **not** the same thing as standing offers in the exchange marketplace. A regular high-alchemy valuation is described as **50% higher** than ordinary low-alchemy conversion value, but **do not derive NPC buy/sell prices** from that relation. Source: [High Level Alchemy](https://wiki.darkan.org/High_Level_Alchemy).

**To implement:** extract the 742 shop definition table with shop owner/NPCs, stock IDs, default/max supply, price source, currency, sell policy, re-stock rates and members/quest gating. Compare against Hagalaz `ShopService` and `ShopScreenScript`; enforce payment and stock movement in a checked operation. Read actual versioned UI component packet operations rather than assuming the current shop screen captures all period functions.

## Data to complete before full parity

The current source set does not provide exhaustive 742 item IDs, widget/config/packet IDs or an executable reference dataset. Capture those from the target client/cache and reconcile with Hagalaz's existing definitions before promoting candidate rules to constants.

[Implementation guide](../shops.md) · [Research policy](../research-evidence.md)
