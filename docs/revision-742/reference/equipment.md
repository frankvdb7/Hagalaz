# equipment — revision 742 research data

**Evidence tier:** period-preserved secondary reference, with pending client/cache verification.

## Historical tool-belt layout and equipment interaction

The ordinary tool belt existed by 742 with **39 slots**, divided among general (hammer, bronze hatchet and pickaxe, chisel, knife, tinderbox, saw, etc.), fishing (nets, rods, lobster pot, harpoon, etc.), crafting (needle and jewellery moulds), and farming (rake, spade, trowel, secateurs) items. It was added **6 December 2011**; a March 2012 change preloaded many ordinary tools. Only **bronze** pickaxe and hatchet are recorded in the historical ordinary belt; advanced tools remain inventory/equipment-specific. Tools are retained on death. Source: [Tool belt](https://wiki.darkan.org/Toolbelt).

**Do not conflate** the separately upgradeable dungeon tool belt with the normal one. See [dungeon belt](https://wiki.darkan.org/Dungeoneering/Toolbelt).

**To implement:** map every 742 equipment slot, two-handed conflicts, defence/attack/strength bonuses, wear level checks and applicable tool ID; persist belt items/state, handle equipment swapping atomically, apply existing check to skills' effective tool queries. Ordinary skill NPC/object item actions need to accept tool-belt supply without spoofing an inventory item.

## Data to complete before full parity

The current source set does not provide exhaustive 742 item IDs, widget/config/packet IDs or an executable reference dataset. Capture those from the target client/cache and reconcile with Hagalaz's existing definitions before promoting candidate rules to constants.

[Implementation guide](../equipment.md) · [Research policy](../research-evidence.md)
