# Repository data ownership and cache joins — revision 742

This is an **audited schema crosswalk**, not a proposed replacement domain model. Property names below were read from the current Hagalaz EF entity classes in October 2026. A property means a defined storage field; it does **not** demonstrate populated rows, valid fixture identities or complete gameplay implementation.

## Gathering and production data owners

| Feature | Current EF entity | Existing fields to reconcile | Build-time join or uncertainty |
|---|---|---|---|
| Mining rock | `SkillsMiningRockDefinition` | `RockId`, `ExhaustRockId`, `OreId` | Cache world-object IDs (16), ore table by `OreId`; inactive object transformation |
| Mining ore | `SkillsMiningOreDefinition` | `ItemId`, `RequiredLevel`, `Experience`, `RespawnTime`, `ExhaustChance`, `BaseHarvestChance` | Cache item definition (19); harvest probability and respawn time units require source check |
| Mining tool | `SkillsMiningPickaxeDefinition` | `Type`, `ItemId`, `AnimationId`, `RequiredLevel`, `BaseHarvestChance` | Cache item (19), sequence (20), effective inventory/equipment/belt tool |
| Woodcutting tree | `SkillsWoodcuttingTreeDefinition` | `TreeId`, `StumpId`, `LogId` | Cache world objects (16) and log item (19), map placement |
| Woodcutting log | `SkillsWoodcuttingLogDefinition` | `ItemId`, `RequiredLevel`, `Experience`, `RespawnTime`, `FallChance`, `BaseHarvestChance` | Source-derived tree timings; distinguish roll per chop versus shared stump |
| Woodcutting tool | `SkillsWoodcuttingHatchetDefinition` | `Type`, `ItemId`, `ChopAnimationId`, `CanoueAnimationId`, `RequiredLevel`, `BaseHarvestChance` | Sequence ID and usable level; note existing `CanoueAnimationId` spelling |
| Fishing method/spot | `SkillsFishingSpotDefinition` | `Id`, `ClickType`, `ToolId`, `BaitId`, `MinimumLevel`, `BaseCatchChance`, `ExhaustChance`, `RespawnTime` | Fishing NPC + menu option via `SkillsFishingSpotNpcDefinition(SpotId,NpcId)`; loot weights by spot |
| Fishing catch | `SkillsFishingFishDefinition` | `ItemId`, `RequiredLevel`, `Experience`, `SpotId`, `Probability` | Join to method by `SpotId`; avoid global fish roll table |
| Fishing tool | `SkillsFishingToolDefinition` | `ItemId`, `FishAnimationId`, `CastAnimationId` | Cache items/sequences; use tool belt eligibility without manufacturing an inventory item |
| Raw cooking | `SkillsCookingRawFoodDefinition` | `ItemId`, `CookedItemId`, `BurntItemId`, `RequiredLevel`, `StopBurningLevel`, `Experience` | Join 742 raw/cooked/burnt IDs; this storage does **not** directly describe different cooking source burn chance |
| Edible food | `SkillsCookingFoodDefinition` | `ItemId`, `HealAmount`, `LeftItemId`, `EatingTime` | Eating behavior differs from cooking; prevent conflation |
| Firemaking | `SkillsFiremakingDefinition` | `ItemId`, `RequiredLevel`, `Experience`, `FireObjectId`, `Ticks` | Ground-fire recipe; **no bonfire recipe discriminator in this entity** |
| Fletching | `SkillsFletchingDefinition` | `ResourceId`, `ToolId`, `ProductIds`, `ProductCounts`, `ProductExperiences`, `RequiredLevels`, `AnimationId` | Product/count/level lists appear serialized as strings; inspect parser and align list indices before modifying schema |
| Crafting: gems | `SkillsCraftingGemDefinition` | `ResourceId`, `ProductId`, `AnimationId`, `RequiredLevel`, `Experience` | Item-based transform |
| Crafting: jewelry | `SkillsCraftingJewelryDefinition` | `ChildId`, `Type`, `ResourceId`, `ProductId`, `RequiredLevel`, `Experience` | `ChildId` binds target selection screen; verify component 742 identity |
| Crafting: leather | `SkillsCraftingLeatherDefinition` | `ProductId`, `ResourceId`, `RequiredResourceCount`, `RequiredLevel`, `Experience` | Requires needle/thread policy outside these columns |
| Crafting: pottery | `SkillsCraftingPotteryDefinition` | `FormedProductId`, `BakedProductId`, `RequiredLevel`, `FormExperience`, `BakeExperience` | Wheel and kiln are **separate** material/XP transitions |
| Crafting: silver | `SkillsCraftingSilverDefinition` | `ChildId`, `MouldId`, `ProductId`, `RequiredLevel`, `Experience` | Child/UI and mould tool identity |
| Crafting: spinning | `SkillsCraftingSpinDefinition` | `ResourceId`, `ProductId`, `RequiredLevel`, `Experience` | Wheel animation/object lookup still needed |
| Crafting: tanning | `SkillsCraftingTanDefinition` | `ResourceId`, `ProductId`, `BasePrice` | Item and coin mutations must commit together |
| Herb cleaning | `SkillsHerbloreHerbDefinition` | `GrimyItemId`, `CleanItemId`, `RequiredLevel`, `Experience` | Separate potion/dose tables are required |
| Prayer offerings | `SkillsPrayerDefinition` | `ItemId`, `Experience`, `Type` | Offering type determines multiplier; ordinary prayer/curses activation is a separate character subsystem |
| Runecrafting | `SkillsRunecraftingDefinition` | `AltarId`, `RuinId`, `PortalId`, `RiftId`, `RuneId`, `TalismanId`, `TiaraId`, `RequiredLevel`, `Experience`, `LevelCountMultipliers`, `AltarLocation`, `RuinLocation`, `RiftLocation` | Parse level count multiplier representation before projecting rune outputs; resolve altar/portal in map cache |

## Spell, familiar, quest, social and economy owners

| Feature | Entity | Existing property names and implementation consequence |
|---|---|---|
| Magic combat | `SkillsMagicCombatDefinition` | `ButtonId`, `BaseDamage`, `RequiredLevel`, `BaseExperience`, `RequiredRunes`, `RequiredRunesCounts`, `CastAnimationId`, `CastGraphicId`, `EndGraphicId`, `ProjectileId`, `AutocastConfig` — UI button to combat spell; the strings require parsing review |
| Magic teleport | `SkillsMagicTeleportDefinition` | `ButtonId`, `SpellBook`, `CoordX`, `CoordY`, `CoordZ`, `Distance`, `RequiredLevel`, `Experience`, `RequiredRunes`, `RequiredRunesCount` — target coordinate/config join |
| Magic enchant | `SkillsMagicEnchantDefinition` | `ButtonId`, `RequiredRunes`, `RequiredRunesCounts`, `RequiredLevel`, `Experience`, `GraphicId` — product mapping separate |
| Summoning | `SkillsSummoningDefinition` | `NpcId`, `PouchId`, `ScrollId`, `ConfigId`, `SummonSpawnCost`, `SummonLevel`, `SummonExperience`, `CreatePouchExperience`, `ScrollExperience`, `Ticks` — **no pouch ingredient/shard list here**, so do not assume historical recipe is fully represented |
| Slayer master | `SkillsSlayerMasterDefinition` | `NpcId`, `Name`, `BaseSlayerRewardPoints` — master reward source; streak multiplier belongs in character task logic |
| Slayer task | `SkillsSlayerTaskDefinition` | `Id`, `Name`, `SlayerMasterId`, `NpcIds`, `MinimumCount`, `MaximumCount`, `LevelRequirement`, `CombatRequirement`, `CoinCount` — `NpcIds` string needs parser/alias audit |
| Slayer progress | `CharactersSlayerTask` | `MasterId`, `SlayerTaskId`, `KillCount` — confirm where streak/reward points persist separately |
| Quest definition | `Quest` | Four prerequisite quests, four skills, four required item slots, four NPC requirements, four XP reward slots, four item reward slots, `RewQuestPoints` — this fixed-width structure may not represent arbitrary long quest graphs |
| Quest progress | `CharactersQuest` | `MasterId`, `QuestId`, `Status`, `Stage` — stage and script state need hydration-path validation |
| Clan | `Clan`, `ClansMember`, `ClansSetting` | Membership/ranks/motifs/settings exist; **no citadel maintenance/resource/weekly tick fields in these classes** |
| Shops | `Shop` | `Id`, `Name`, `Capacity`, `CurrencyId`, `MainStockItems`, `SampleStockItems`, `GeneralStore` — stock strings and replenishment semantics require parse/source inspection |
| Item catalog | `ItemDefinition` | `Id`, `Name`, `Examine`, `TradePrice`, `LowAlchemyValue`, `HighAlchemyValue`, `Tradeable`, `Weight`; stack/note/equipment semantics belong to other definitions and cache |
| NPC catalog | `NpcDefinition` | `NpcId`, `Name`, `RespawnTime`, `CombatLevel`, `AttackSpeed`, attack/defence/death graphics, `NpcLootId`, `NpcPickpocketingLootId`, `SlayerLevelRequired` — **definition is not spawn map** |
| Ground spawn | `ItemSpawn` | `ItemId`, `Count`, `CoordX`, `CoordY`, `CoordZ`, `RespawnTicks` |
| Lodestone | `GameobjectLodestone` | `GameobjectId`, `ButtonId`, `StateId`, `CoordX`, `CoordY`, `CoordZ` — direct UI/state/map binding |

## Persistence-specific crosswalk

- `CharactersFarmingPatch` stores `MasterId`, `PatchId`, `SeedId`, `ConditionFlag`, `CurrentCycle`, `CurrentCycleTicks`, `ProductCount`. `SkillsFarmingSeedDefinition` stores `ItemId`, `ProductId`, `MinimumProductCount`, `MaximumProductCount`, `RequiredLevel`, `PlantingExperience`, `HarvestExperience`, `VarpbitIndex`, `MaxCycles`, `CycleTicks`, `Type`. An offline-growth migration requires inspecting actual time interpretation, not assuming this model has an absolute timestamp.
- Character item collection and familiar state use the existing hydrator/dehydrator pipeline and snapshot-revision persistence; see `docs/character-persistence.md`.
- The absence of a dedicated marketplace order, house instance, or Runespan runtime entity in this table does **not** prove that it has no backing storage anywhere. Search the complete repository and migrations before designing a new persistent owner.

## Repeatable join procedure

For each researched CSV, map `historical name -> exact 742 cache ID -> Hagalaz EF definition row -> runtime service -> script -> widget/packet`. Populate every link with the **source path, revision cache checksum, IDs, units and evidence status**. If an edge cannot be proven, leave it unresolved, **never infer an adjacent ID**.

### Suggested review invariants

1. Every cache numeric ID must have a matching expected kind (item vs world object vs NPC vs animation vs widget).
2. All names and levels in a secondary-source CSV are candidates until both target cache and actual Hagalaz data are inspected.
3. Every item/output transfer must go through the existing authoritative item-container boundary.
4. Every persisted gameplay timer must state whether it is absolute wall time, game ticks, or remaining ticks after hydration.
5. Strings holding parallel lists must be parsed and validated by the owning repository/service; no indexing beyond unequal lengths.
6. For a completed feature, store fixture/checksum and positive/negative/cancellation/restart test evidence in its feature reference.

See [client/cache evidence](client-cache-evidence.md), [data pack](../data/researched/README.md), and the [engineering conventions](../implementation-conventions.md).
