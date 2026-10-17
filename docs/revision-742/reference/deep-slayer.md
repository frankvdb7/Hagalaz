# Slayer: task assignment, reward economy and persistence

## Dated 2012 reward source

The archived [Slayer points reference](https://wiki.darkan.org/Slayer_points) reports its **last edit as 2 November 2012**, close to the target build, making it a valuable period source. [Master points table](../data/researched/slayer-master-points.csv) and [purchase/unlock table](../data/researched/slayer-point-rewards.csv) contain the listed values.

**Important exception:** The source describes 5x for each tenth and 15x for each fiftieth task in general, **but Mazchna/Achtryn is printed as 2 standard / 5 tenth / 15 fiftieth**, not arithmetic 10/30. Use explicit source override until cache or live fixture proves whether the table is a typo. Do not impose the generic multiplier on that row.

The source also documents Social Slayer co-op introduced **11 July 2012**. Co-op task completion can provide separate co-op points, partner healing and potion-effect sharing unlocks. This requires a separate state/data review: current Hagalaz Slayer task model alone does not establish co-op support.

## Existing data boundaries

- Master: `SkillsSlayerMasterDefinition` has `NpcId`, `BaseSlayerRewardPoints`.
- Assignment: `SkillsSlayerTaskDefinition` has `Id`, `SlayerMasterId`, `NpcIds`, `MinimumCount`, `MaximumCount`, `LevelRequirement`, `CombatRequirement`, `CoinCount`.
- Progress: `CharactersSlayerTask` has `MasterId`, `SlayerTaskId`, `KillCount`.
- Runtime: `Hagalaz.Services.GameWorld/Model/Creatures/Characters/Slayer.cs` includes reward-calculation TODOs. Look up streak and reward currency persistence before adding fields.
- UI: `Hagalaz.Game.Scripts/Skills/Slayer/` assignment, master dialogue and reward screens. Client widgets must supply UI intent only; server validates ranks and costs.

## Full assignment lifecycle

1. **Unlock:** Smoking Kills task/personal quest stage enables point accumulation after the fourth consecutive qualifying assignment.
2. **Assign:** master requirement and candidate monster list, prior task/blacklist, zone restrictions, weights and count ranges. Server chooses; no UI authority on NPC ID or reward quantity.
3. **Kill:** attribute one credited kill (NPC family/variant and kill owner) against active task. Keep counts nonnegative, and don't credit party helpers without period evidence.
4. **Finish:** resolve exactly one terminal transition; update streak and base/bonus points once. A Turael/Spria replacement can reset streak as documented. Persist before public reward success message.
5. **Shop:** purchase runes/arrows, skip/block task, crafting unlocks, pets, co-op upgrades. Deduct points and add reward/unlock atomically. The source states point cap 64,011; verify current Hagalaz storage width and 742 behavior.
6. **Co-op:** if implemented, use existing party/character messaging contracts; define joint task identity, separate progression, partner logout and point settlement before changing task counters.

## Reward/requirements highlights

The source lists ring (8 charges) 75 points; 250 Magic Dart casts 35 points; slaying ring crafting 300 points/75 Crafting; broad ammunition unlock 300 points; helmet unlock 400 points/55 Crafting; skip 30; block 100; and a **six-block** maximum conditioned on quest points (one per 50 up to 300). These are candidate period rules and may have item/policy subtleties.

## Required test fixtures

Kill credit same NPC twice; two killers of same target; kill allowed variant while assigned; task replacement near task 10 or 50; tasks 1–5 without unlock; 10th and 50th for every master including exceptional row; disconnected player at reward time; task block/unblock; points spent with insufficient balance; co-op partner leaves; reward claim repeated; missing/damaged quest flag.

**Missing** weighted task pool by master, monster variant map, period drops and task-specific XP, exact item reward IDs, unlock configuration IDs and persisted streak/reward/collaboration state.
