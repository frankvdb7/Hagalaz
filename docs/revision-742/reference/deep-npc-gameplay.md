# Revision 742 — NPC gameplay, loot, combat and interaction evidence

**Source:** current Hagalaz classes anchored to [`9c9f8622305d`](https://github.com/frankvdb7/Hagalaz/commit/9c9f8622305d18ca05fa7099848a0cdb01cf09b6). Existing [383 script-registered NPC IDs](../data/catalogs/npc-script-registrations.csv) are not every client NPC and not evidence of spawn placement. See [104 directly extracted pickpocket NPC rows](../data/catalogs/npc-pickpocket-source-rules.csv). No populated loot table, encounter snapshot or full index-18 export was available.

## 1. Cache definition, DB statistics and runtime NPC

- Cache **18**, shard `id >> 7`, file `id & 127`; `NpcTypeProvider` / `NpcTypeCodec` decode name, actions, models, size, combat-level presentation, transformations, varp/varbit selectors, render/idle/walk/run animations, map icon and more.
- `Hagalaz.Data/Entities/NpcDefinition.cs` overlays server-only `Attackable`, `ReactionType`, `BoundsType`, `WalksRandomly`, `RespawnTime`, `AttackSpeed`, attack/defence/death animation/graphic, death ticks, loot table IDs and Slayer requirement. `NpcStatistic` stores HP and skill levels; `NpcBonuses` stores attack, defence, damage and absorption modifiers. DB values must come from a versioned snapshot, not fabricated defaults.
- `NpcSpawn` is a **placement instance** with coordinates and movement bounds; `NpcProfile` projects it into world dimension 0. Additional encounter/familiar spawns are dynamic and are separately catalogued in [spawn origins](../data/catalogs/npc-spawn-sources.csv).

## 2. Weighted NPC drop tables: exact runtime semantics

The Hagalaz tables are `npc_loot` and `npc_loot_items`, linked from `npc_definitions.npc_loot_id` and `npc_pickpocketing_loot_id`. Each loot item has `item_id,minimum_count,maximum_count,probability,always`; table has `maximum_loot_count,randomize_loot_count,always` and possible child tables. `LootStore.LoadAsync` constructs `LootTable` and links child entries after all tables have loaded. Rare-labeled tables can receive a `RingOfWealthModifier`. **The `probability` column is a relative weight for ordinary candidates, not by itself an independent percentage**: `LootGenerator.GenerateLoot` filters enabled non-always entries, applies modifiers, sums their probabilities and draws weighted selections. Always entries are included independently of `MaxCount`; the code can randomize remaining roll count; nested tables need recursive inspection. Count ranges apply after a selection and must not be misread as extra rolls.

`NpcCombat.OnKilledBy` uses `Definition.LootTableId`, `CharacterLootParams`, generator and a ground-item owner. `StandardPickPocketTask` uses **`Definition.PickPocketingLootTableId`**, not the kill table, and adds loot to player inventory after success. Do not conflate the two IDs or use a generic unowned ground drop for pickpocketing.

**Concrete unresolved correctness risk:** current `LootStore.LoadAsync` visibly sets `MaxResultCount` and `RandomizeResultCount` on **game-object and item** loot tables, but the NPC table initializer shown in source omits these. `LootParams.MaxCount` defaults to `Table.MaxResultCount`; `LootGenerator` then has no normal weighted rolls when the default is zero, although `Always` entries can still appear. Check full initialization and add a failing regression against a real NPC table before any code fix. Classify this as **source-derived hypothesis requiring test**, not confirmed live malfunction.

For a complete export, publish distinct `npc-loot-tables-742.csv`, `npc-loot-items-742.csv`, `npc-loot-child-edges-742.csv` and `npc-pickpocket-loot-joins-742.csv`. Preserve numeric decimal weights, `Always`, max/min count and child edges; detect missing items, empty non-always tables, orphaned child IDs and nested cycles. Run Monte Carlo tests only as supporting distribution checks, not substitutes for deterministic seeded tests.

## 3. Combat, aggression and death

`NpcCombat` delegates attack bonus/style/range/speed to `INpcScript`, uses `NpcStatistics` and bonuses for hit/damage, and handles target validity, attacker accounting, death animation, kill credit, drops and respawn/unregister. `NpcScriptBase` provides melee defaults; `MagicNpcScriptBase`, `RangedNpcScriptBase` and each encounter override have distinct projectile/graphic/damage and phase contracts.

Default aggression via `NpcScriptBase.IsAggressiveTowards` checks player target, distance `GetAttackDistance()+2`, occupied single-combat targets, recent attackers and reaction-type/combat-level gating. `AggressivenessTick` chooses a nearest eligible visible player for `Aggressive` or `CombatAggressive`. **A subclass may override all of this** (e.g. faction or boss behavior): do not propagate default combat rules into boss records without inspecting each subclass.

Death: `NpcCombat.OnDeath` schedules animation delay, winner attribution, loot and either script-controlled respawn after `RespawnTime` or unregister. `OnKilledBy` depends on `CanBeLootedBy`, so loot presence is not enough to promise a drop. NPC movement/pathfinding, line-of-sight, height plane, collision, PvP/pet ownership and combat state need runtime tests.

## 4. Pickpocketing — actual source-backed rules

[104 NPC ID rows across 18 archetypes](../data/catalogs/npc-pickpocket-source-rules.csv) were extracted directly from `Hagalaz.Game.Scripts/Skills/Thieving/Thieving.cs` `PickPocketDefinition[] Ppd`. Fields contain min Thieving level, success XP, fail damage, minimum levels for double loot and source group. The special sentinel **255** in the last two archetypes means the source supplies an effectively unavailable extra-loot threshold, **not** that their actual level requirement is 255.

`StandardPickPocketTask.OnInitialized` checks Thieving **and** Agility threshold, then +10/+20 on each for triple/quadruple loot. Animation IDs: pick **881**, double **5074** / graphic **873**, triple **5075** / **874**, quadruple **5078** / **875**. On failure `OnPerform` plays target attack animation and performer stun animation **424**, stun for **6** task units, deals configured fail damage. Its current success comparison uses Thieving plus one quarter of Agility plus one against a level-derived random threshold; source exact formulas must be retained in characterizations, not replaced with a wiki success rate. XP is awarded after asynchronous loot service retrieval and cancellation check; review semantics if the table is missing. The actual loot **items and weights** live in the DB and were not exported.

Review the [historical thieving candidates](../data/researched/thieving-pickpocket-targets.csv) as a **separate historical evidence tier**. Where levels/XP conflict with this source, record both and investigate the pinned cache/client; never silently merge or promote one to verified behavior.

## 5. Dialogue, shops, quests and encounter-specific NPCs

Source routes:
- `NpcScriptBase.OnCharacterClick*/UseItemOnNpc` and `NpcClickType` dispatch menu operations; `Hagalaz.Game.Scripts/Npcs/Shops/*`, `Bankers/*`, `Areas/*/Npcs/*` own scripts/interaction dialogue. Verify menu-option ordering in the actual 742 interface and packet. A method existing does not mean a client option can invoke it.
- `ShopService` reads `Shop` from `Hagalaz.Data/Entities/Shop.cs` and constructs `ShopStockContainer` with currency/main/sample stocks. `Services/Model/Shop.cs` currently returns buy `ItemDefinition.Value` and sell 90% of that value, min 1, before feature-specific modifiers. Do not assume stock, price or currency from NPC names.
- Quest dialogue and routing may require `CharactersQuest`, `Quest`, stage and conditional NPC transformations; cache `QuestIDs` is descriptive metadata, not a complete stage graph.
- Bosses and factions live in `Minigames/Godwars`, `Minigames/TzHaar`, `Npcs/Bosses`, `Npcs/Dragons` and `Npcs/Elementals`. Capture phase transitions, minions, prayers/weakness, target selection, attack cycles, projectiles, animation/graphic/audio, hard bounds, entrance conditions, drop rolls and kill credit **per encounter**. Current source registration ID 5362 is claimed by both `BrutalDragon` and `StandardDragon`; the first `NpcScriptProvider.TryAdd` wins and logs a warning for overlap. Test selected runtime script explicitly.

## 6. Minimal complete per-NPC record and verification

Produce a joined `npc_id,cache_raw_hash,cache_name,actions,combat_level,size,transform_ids,config_selectors,model_ids,render_anims,animation_ids,server_db_present,stats_present,bonuses_present,attack_speed,attack_style,attack_range,aggression_rule,death_delay,respawn_ticks,kill_loot_table_id,pickpocket_loot_table_id,shop_id,dialogue_script,quest_gates,encounter_script,spawns,evidence`. A single NPC ID can have different effective transforms and multiple spawn contexts; preserve separate source tables and instance IDs.

Required tests: attacker range/LOS, minion spawn count, faction aggression/multicombat, no loot on inaccessible/unowned kill, exactly-once drop on death, seeded weighted drop and Always effects, nested loot cycle guard, pickpocket thresholds (double/triple/quadruple, 255 sentinel), inventory full and cancel, stale NPC handle/click options, shop stock/payment conservation, boss phase cleanup, restart state and no ghost NPC registrations.

**Not done:** complete physical index-18 cache definitions, populated loot item lists and weights, all shops/quest dialogue mappings, live NPC spawn coordinates, every boss's historical mechanics and packet parity. This dossier records source ownership and tests to finish those gaps, not completion of the entire gameplay census.

## 7. Source-ID coverage and next data joins

The [104 NPC pickpocket mapping records](../data/catalogs/npc-pickpocket-source-rules.csv) preserve one numeric NPC per explicit `PickPocketDefinition` row, including source-listed cave goblin ID `5768`. Compare with [static NPC script registration IDs](../data/catalogs/npc-script-registrations.csv) to find scanner omissions or factory precedence; do not infer actual spawn coverage. After supplying populated DB rows, export `npc_loot` and `npc_loot_items` **with table count, child links, probability and Always semantics** and test `LootStore`'s NPC max-result initialization before claiming weighted roll parity.
