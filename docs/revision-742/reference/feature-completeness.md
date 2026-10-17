# Feature completeness, source conflicts and work needed for actual parity

The [41-feature matrix](feature-completeness-matrix.csv) is the **minimum complete dataset specification**, not a statement that each domain's data has already been assembled. The curated CSVs are a significant step, but full implementation requires 742 cache IDs, game-world row coverage, packet/CS2 mappings and live behavior fixtures.

## What each feature must deliver to be considered implementation-ready

| Evidence bundle | Definition of ready |
|---|---|
| Content manifest | Every expected NPC, item, object, widget, animation, quest/area or recipe row for the **selected feature subset** has a key and verified version evidence |
| Rule record | Every skill gate, prerequisite, XP/cost/reward formula, timing, success/failure probability, exclusive state and persistence policy is explicit |
| Interaction trace | For each action: target cache record, client click/CS2, outgoing (client) packet, server decoder/handler/script, game mutation, incoming (client) update |
| State ownership | Existing Hagalaz service/model/EF table responsible for data, live state and save/restore is named; no duplicate owner |
| Acceptance fixtures | Success, invalid input, capacity, interrupt, stale entity/item identity, two-player race, repeated callbacks and restart state are all reproducible |
| Content provenance | Source URL, dated source revision, target cache checksum, item/NPC/object IDs, evidence grade and unresolved conflicts are stored |

## Action inventory to use when completing a specific subsystem

**Production skills:** every recipe needs `skill_id,action,source_item_ids/counts,tool_ids,output_item_ids/counts,level,xp,animation,tick_duration,success/fail_roll,ui_button,condition`. Cooking burn/fishing catch/woodcutting fall are different probability models. Enchanting/Runecrafting can use level-dependent output multipliers.

**Creature encounter:** NPC source definition, spawn/map identity, effective stats/defence, aggression range, attack style/speed, animation/projectile, damage/accuracy, special attacks, drops/loot visibility, respawn, slayer/task conditions and multi-target ownership.

**Item economy:** item/note/stack/charges/wear requirements, input/output item instances, currency source, market price type, recipient capacity and storage. Snapshot and rollback boundaries must be declared for trade, duel and marketplace orders.

**Quest and progression:** dependencies, stages, varp/varbit state, dialogue branches and trigger graph, world changes, quest item behavior, journal states, reward idempotency and persistence.

**Spatial activities:** region ID, map-square archive, collision, object IDs and shape/rotation, path target, instance owner, respawn/cleanup, scene updates and dangerous/safe area rules.

**Social and periodic tasks:** relationship ranks/permission matrix, message routing, guild instances, daily/weekly reset semantics, ownership and idempotency.

## Explicit source policy

- Prefer matching revision cache and unchanged client behavior over all secondary material.
- A date-stamped 2012 article is period evidence, but not proof that each stored number is correct or all variants exist.
- Retrospective/current websites often silently merge several releases; quarantine later additions.
- The companion Hagalaz.GameClient pinned cache exists, but **its large Git LFS payload has not been decoded in this PR**. Never call a guessed ID directly verified.
- `docs/revision-742/reference/deep-*.md` documents the first genuinely deep vertical flows. Extend those by direct code/cache evidence, not by creating a new speculative gameplay infrastructure layer.

## Recommended extraction order

1. **Item/NPC/object/cache inventory:** a machine-queryable export of indices 19, 18, 16, then validate item note/stack, NPC combat/options and object actions.
2. **Widgets+CS2 and packet family:** derive exact components and option dispatch for bank, inventory, skills, prayer, magic, familiars, shops, trade and quests.
3. **Small production tables:** map recipes in current SQL/EF definitions for mining, woodcutting, fishing, cooking, firemaking, smithing, fletching and herblore.
4. **World/script content:** per NPC/area quest and minigame interactions, object spawns and collision.
5. **Missing durable systems:** marketplace standing offers, house layout, dungeon party instance, Runespan and periodic challenges.
6. **Full behavior validation:** run chosen scenarios with target client and persistent server, recording exact outgoing updates and save/restart outcomes.

This file deliberately identifies a **large remaining amount of work**. The present research pack makes implementations faster and less error-prone; it does not magically recover every NPC/quest/spell/recipe in the world.
