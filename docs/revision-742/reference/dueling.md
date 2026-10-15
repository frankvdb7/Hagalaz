# dueling — revision 742 research data

**Evidence tier:** period-preserved secondary reference, with pending client/cache verification.

## Period gameplay

Duel Arena offers **friendly and staked** one-on-one matches with restrictions selected before combat. The duel arena is **safe on death**, except the voluntarily escrowed stake can be awarded to the winner. A three-two-one countdown precedes combat. A **No Forfeit** option controls whether the player may surrender via arena exits. Source: [Duel Arena](https://wiki.darkan.org/Duel_Arena).

**To implement:** extract 742 option IDs and legal combinations (combat style, movement, food, prayer, magic, equipment, inventories and stake limits where applicable) and safe-death arena region. Stake ownership and recipient capacity are handled with existing duel exchange transactions. Confirm rules when starting and enforce again at each combat action. Distinguish forfeiting, disconnecting, tie handling, opponent death and restricted objects. Several preserved discussions reference historical limits from 2007–2008; they must not be assumed to apply in 742.

## Data to complete before full parity

The current source set does not provide exhaustive 742 item IDs, widget/config/packet IDs or an executable reference dataset. Capture those from the target client/cache and reconcile with Hagalaz's existing definitions before promoting candidate rules to constants.

[Implementation guide](../dueling.md) · [Research policy](../research-evidence.md)
