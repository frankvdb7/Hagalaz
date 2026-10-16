# Revision 742 — item identity, relations and gameplay integration

**Evidence:** Hagalaz server/cache source audited at [`9c9f8622305d`](https://github.com/frankvdb7/Hagalaz/commit/9c9f8622305d18ca05fa7099848a0cdb01cf09b6). Cache definition presence, full 742 item IDs, database rows and historical behavior are **not independently verified** because the pinned fixture and populated DB were not available. This is an implementation dossier and exact-source decoder/contract reference, not an exhaustive data export.

## 1. Source of truth and three distinct identities

- **Type:** `Hagalaz.Cache/Types/ItemType.cs` decoded by `ItemTypeCodec`, index **19**, shard `group = id >> 8`, `file = id & 255`. Export only present reference-table subfiles, not `ArchiveSize` placeholders.
- **Server override:** `Hagalaz.Data/Entities/ItemDefinition.cs`, joined in `ItemDefinitionProvider`. Fields: name, examine, trade price, low/high alchemy value, tradeable flag, weight. The AutoMapper `ItemProfile` keeps explicit server fields separate from cache properties.
- **Runtime item instance:** ID + stack count + `ExtraData`/equipment/container ownership. Charge state and lifetime are **instance** properties; don't assign charge counts to a static type row or equate instance identity with definition ID.
- Useful numeric script references: [281 source-registered item IDs](../data/catalogs/item-script-registrations.csv). These are script attributes, not a complete cache listing.

## 2. Noted/unnoted, stacking and dynamic model variants

`ItemTypeCodec.Decode` reads opcode **11** (`StackableType=1`), **65** (`Unnoted`), **97/98** (`NoteId/NoteTemplateId`), **100–109** (`StackIds/StackAmounts`), **121/122** (`LendId/LendTemplateId`), **139/140** (`BoughtItemId/BoughtTemplateId`), and opcode **132** (`QuestIDs`).

**Never infer note status from display text or adjacency.** `ItemTypeLogic.IsNoted` is `NoteId != -1 && NoteTemplateId != -1`. `IsStackable` is `StackableType == 1 || IsNoted`. `ItemType.MakeNote(item,template)` inherits the original item's name/value/membership and uses template inventory model/appearance; it forces stacking. `MakeLend` and `MakeBought` are separate projection paths, not notes. A lending variant adopts original worn/head meshes and equipment slots and defines `Discard` in its last inventory option. Validate **both ends** of every reference and distinguish `-1` from valid zero.

For exact-cache export, use one row per **populated item ID** in `item-definitions.csv`; one row per directed `(from_id,relationship,to_id,slot_or_threshold)` in `item-relationships.csv`. Relationship kinds: `note_item`, `note_template`, `lend_item`, `lend_template`, `bought_item`, `bought_template`, `stack_model`. A blank relationship is not an ID of zero. Preserve raw opcode and source SHA for each edge; do not deduce reverse edges without matching decoded data.

For stacking, exercise inventory, bank, trading, shops, money pouch, item-on-ground and note/un-note with partial capacity, integer boundaries, duplicate slots and simultaneous transfers. Reuse current `ItemContainerTransaction`/container mutation ownership; no replacement orchestration layer. Test rollback on downstream failure and exact item conservation.

## 3. Equipment constraints

Decoded item fields include `EquipSlot` (opcode 13), `EquipType` (14), `SomeEquipInt` (27), gender-specific worn/head models (23–26, 78–79, 90–93), wear offsets (125/126), render animation and extras. `ItemTypeLogic.GetEquipmentRequirements` interprets extra-param pairs **749/750**, **751/752** … **767/768** as skill ID and level, accepting skill below 25 and level up to 120; it also applies `MaxedSkillRequirement` with the source's special 19709/120 versus 99 rule. Do not assume all requirements are encoded on one item-type opcode. `GetQuestId` reads a separate extra-param key, while `QuestIDs` is a distinct opcode-132 array. Cross-check both when enforcing quest gates.

Server combat/equipment overlays also appear in `Hagalaz.Data/Entities/EquipmentDefinition.cs` and `EquipmentBonuse.cs`. Their attack/defence/absorb/damage, attack distance and attack/defence animation fields are **not** proof of filled DB rows. Existing `EquipmentContainer`, `EquipmentService`, equipment scripts and activation checks should decide equipment conflicts, offhand/two-handed exchanges and callbacks. Record failed equip attempts without item loss or stale state.

## 4. Degradation and charge families

These are deliberately separate rule systems:

| Family | Existing Hagalaz owner | State transition and tests |
|---|---|---|
| Source-defined drop degradation | `ItemTypeLogic.GetDegradeType`, extra param `ItemConstants.DegradeType` | Default `DegradeType.DropItem`; source enum semantics, don't assume per-use charge counter |
| Combat tick/death degradation | `Hagalaz.Game.Scripts/Equipment/DegradingEquipment.cs` | `item.ExtraData[0]` as remaining ticks; decremented after attacks, multiplied by death factor; replace exact equipped instance or crumble |
| Timed degradation | `Equipment/Degraded/DegradeTask.cs` | Save remaining delay on equipment change/logout; verify task cancellation and instance ownership |
| Item family-specific transitions | `Equipment/Barrows/*`, `Equipment/Zaros/*`, `Equipment/Corrupted/*` | Variant paths and script attributes must be exported as actual state graph, not a one-size-fits-all durability rule |
| Auras and temporary effect state | `Items/Auras/AuraItemScript.cs`, `Equipment/Auras/AuraEquipmentScript.cs` | Activated vs deactivated ID, cooldown and equipment rejection are distinct from ordinary degradation |

Concrete source example: `DharokEquipment.rseq.cs` lists 20 equipment IDs (four slots by five stages), yields a first degradation transition after zero initial ticks, then uses **22500** remaining ticks for later stages, and a **0.80** death factor. These are **current Hagalaz implementation constants**, not verified historical timings. Verify the whole 20-ID path before importing it as period-correct behavior. The generic `DegradingEquipment` re-equip/replace path must be tested for exact-instance matching and atomic publication.

## 5. Consumables and per-item interactions

`Hagalaz.Game.Scripts/Skills/Herblore/Potions/Potion.cs` obtains the **ordered four IDs** from `ItemScriptMetaData`: full, 2/3, 1/3, final dose, then empty vial. It calls `IPotionSkillService.DrinkPotion` only after `DrinkAllowEvent`, stun/drinking-state checks and interruption; item-on-item calls `CombinePotions`. Do **not** sort these four IDs numerically or treat every consumable as a potion. Inspect eating/cooking, tele-tabs, necklaces/rings, scrolls, tool-belt items, pouches and quest items separately. Record `input_id,output_id,source_container,requirement,animation,effect,duration,remaining_doses,cancel_behavior`.

Inventory/ground `InventoryOptions` are cache data (35–39/30–34); server `ItemScript`/equipment scripts implement effects. A cache option or registered script does not prove a particular widget-click operation reaches it. Follow client widget/CS2 and outgoing packet to server dispatch. Item-on-item, item-on-NPC, item-on-object and equip/unequip each need separate authorization and validation of source item **instance**, target, range, quantity and inventory capacity.

## 6. Recommended export and test fixtures

Use the [read-only archive/DB reconciliation tooling](../tools/README.md) as an initial join. Create explicit incremental outputs:

- `items-742.csv`: `id,cache_sha,raw_sha,index,group,file,name,stackable_type,note_id,note_template_id,lend_id,lend_template_id,bought_item_id,bought_template_id,equip_slot,equip_type,member,quest_ids,inventory_options,ground_options,extra_params`.
- `item-relationship-edges-742.csv`: one source-evidenced edge per variant/threshold, with destination-presence check, cycle handling and transformation reason. Do **not** force reciprocals for templates.
- `equipment-rules-742.csv`: equipment requirements, two-hand/slot conflicts, bonuses, animations, wear models and relevant quest gates from decoded cache plus versioned DB.
- `item-effect-rules-742.csv`: potion dose chain, food residual, charge/use-on transforms, price/trade/death behavior. Distinguish DB/source-script rules from client observation.

Regression set: stack overflow/underflow; note conversion preserving item counts; missing template and variant loops; partial bank and inventory; loan/degrade/delete state on logout; equipment set bonuses after unequip; potion combine with odd quantities; untrusted item-click slots; quest restriction in both client presentation and server enforcement; concurrent trade/duel/equipment integrity.

**Not done:** full cache-wide link extraction, current populated DB equipment requirements/bonuses, comprehensive item recipes, live UI/packet parity and historical validation. Do not report these complete solely from the 281 script registrations.
