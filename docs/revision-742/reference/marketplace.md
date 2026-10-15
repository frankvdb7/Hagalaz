# marketplace — revision 742 research data

**Evidence tier:** period-preserved secondary reference, with pending client/cache verification.

## Historical matching rules and interfaces

- **Free accounts:** 2 concurrent offer slots. **Members:** 6.
- Buy/sell orders support partial fills; completed coins/items can be claimed from qualifying banks.
- Buyer limit applies to specific items over a **four-hour** window, but **limits vary by item**. Do not create a single universal limit.
- Offers match when bid >= ask; the **earlier resting offer's price** governs the exchange according to the preserved description.
- Equal-price competition is chronological; no direct value restrictions after **1 February 2011**. Collection can occur at many banks but not most deposit boxes.
- Guide-price change buttons support **±5%**, as well as manual offered price.

Source: [Exchange marketplace](https://wiki.darkan.org/Grand_Exchange).

**To implement:** persist order owner/slot, item, side, unit limit price, original/remaining quantity, reserved value, matched value, claimable items/coins, placement time and priority; matching must be atomic/idempotent and maintain correct price improvement refund to buyer. Add explicit 742 order submission/modify/cancel/collect screen semantics and tax policy (**no later tax unless verified for target**). Extract full per-item tradeability and four-hour buy-limit tables separately.

## Data to complete before full parity

The current source set does not provide exhaustive 742 item IDs, widget/config/packet IDs or an executable reference dataset. Capture those from the target client/cache and reconcile with Hagalaz's existing definitions before promoting candidate rules to constants.

[Implementation guide](../marketplace.md) · [Research policy](../research-evidence.md)
