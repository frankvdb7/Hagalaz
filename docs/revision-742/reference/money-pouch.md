# money-pouch — revision 742 research data

**Evidence tier:** period-preserved secondary reference, with pending client/cache verification.

## Period behavior

- Released **6 December 2011**; capacity **2,147,483,647 coins**.
- Can receive looted, picked-up, quest/reward and bank-withdrawn coins directly, subject to a user's toggle.
- Toggle-off routes new coins to inventory while existing pouch coins stay in pouch.
- Withdrawal allows entering an amount; examination reports the balance. A bank deposit-coins option was added **4 January 2012**.
- The pouch is protected from dangerous-death drops and historically bank-PIN protected.
- Many shop payments and related coin sinks can draw from the pouch.

Source: [Money pouch](https://wiki.darkan.org/Money_pouch).

**To implement:** create a matrix of every coin ingress/egress route, compare with Hagalaz's single pouch owner, and reconcile over-cap behavior with available backpack space. Use signed-int guard checks and an item-container transaction for cross-container transfers. Do not assume toggle semantics until widget/config capture verifies them.

## Data to complete before full parity

The current source set does not provide exhaustive 742 item IDs, widget/config/packet IDs or an executable reference dataset. Capture those from the target client/cache and reconcile with Hagalaz's existing definitions before promoting candidate rules to constants.

[Implementation guide](../money-pouch.md) · [Research policy](../research-evidence.md)
