# banking — revision 742 research data

**Evidence tier:** period-preserved secondary reference, with pending client/cache verification.

## Historical capacity and tabs

| Entitlement described | Bank slots |
|---|---:|
| Free, no registered email | 78 |
| Free, registered email | 88 |
| Member baseline | 556 |
| Member with one booster | 606 |
| Member with five boosters | 806 |

Historical banks offered **up to eight tabs**; the first item in a tab supplies the tab icon. Most items stack while banked even if not stackable in backpack. The above capacities are entitlement-sensitive, not a safe hardcoded constant for every Hagalaz character. Source: [Bank](https://wiki.darkan.org/Bank).

**To implement:** verify membership/email/booster policy desired for Hagalaz, 742 component IDs and capacities and item-stack representation. Implement exact deposit/withdraw note, tab reorder, deposit inventory/equipment/familiar and coin deposit with transactional conservation. Guard against invalid preferred slots, full capacity, duplicate interface callbacks, concurrent bank operations and logout persistence.

## Data to complete before full parity

The current source set does not provide exhaustive 742 item IDs, widget/config/packet IDs or an executable reference dataset. Capture those from the target client/cache and reconcile with Hagalaz's existing definitions before promoting candidate rules to constants.

[Implementation guide](../banking.md) · [Research policy](../research-evidence.md)
