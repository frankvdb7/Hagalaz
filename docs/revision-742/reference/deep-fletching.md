# Fletching: structured 2012 candidate recipes and execution contracts

## Candidate data

- [fletching-bow-stages.csv](../data/researched/fletching-bow-stages.csv): 13 records.
- [fletching-crossbow-stages.csv](../data/researched/fletching-crossbow-stages.csv): 7 records.
- [fletching-arrow-candidates.csv](../data/researched/fletching-arrow-candidates.csv): 15 records.
- [fletching-bolt-candidates.csv](../data/researched/fletching-bolt-candidates.csv): 26 records.
- [fletching-dart-candidates.csv](../data/researched/fletching-dart-candidates.csv): 7 records.

Source: https://wiki.darkan.org/Fletching_boost. These are source-derived historical candidate values, not verified build 742 item IDs. A row's empty item_id means no verified cache mapping; it is not item zero. High-tier bane/dragon variants need explicit build-availability checks.

## Hagalaz owners and implementation

- Hagalaz.Game.Scripts/Skills/Fletching/FletchingSkillService.cs handles item-use dispatch; FletchingDefinition.cs and FletchingTask.cs own existing production, with SkillsFletchingDefinition persisted recipes. Inspect parser alignment across product IDs, counts, XP and levels before changing data.
- Bows are TWO separate transformations: logs to unstrung output, then bowstring and unstrung output to completed bow; each grants its own XP and validates its own tool and resource.
- Crossbows have THREE stages: stock carving, limb attachment and stringing. The source's 'total XP' is only the sum of these three individually awarded actions.
- Arrow/bolt quantities differ (15 regular arrows; 10 most bolts; 6 kebbit variants). Source reported per-item and per-batch XP do not always multiply evenly (rounding and cumulative-stage reporting): never award both summed and listed totals.
- Smithing generates unfinished bolt/dart tips and awards its own XP. Finishing projectile ammunition is a different Fletching mutation, and enchanted ammunition is Magic.
- Quest restrictions: The Tourist Trap for darts; Slayer-point knowledge for broad ammunition. Verify specific quest gates and 742 client item availability rather than trusting display labels.
- Revalidate current exact item instance, total input counts, target slot capacity, tool access and still-selected recipe on every repeated task iteration. Use the existing atomic mutation boundary; grant XP only for committed output. A stale callback must not start duplicate task instances.

## Source disputes and missing direct evidence

The initial data/fletching-bows.csv leaves ordinary shortbow level unknown; the new source prints 1. Do not overwrite that conflict without a matching client/skill guide observation. The bolt table contains an anomalous 'mith grapple 59 XP' that was intentionally NOT converted into a standard ten-bolt recipe. The two significant arrow and per-bolt experience columns report separate source units; do not coerce or silently round them. Numeric item, note, interface, animation, config, click and material IDs remain unset. For each row, reconcile a pinned index-19 item definition, 742 item-on-item action and Hagalaz EF record before enabling it.

## Deterministic test cases

For 13 bow products, seven crossbows and all relevant projectiles: one action, batch N, insufficient ingredients, single missing tool, noted inputs, full destination, stale slot, incorrect mould/staff, incomplete quest, changed selected product, repeated callback, interrupt, logout and save/reload. Assert item conservation and exactly-once XP after each successful unit, with no XP on rejected action. Keep existing task/container ownership; no additional global production framework.

[Feature guide](../fletching.md) · [cache extraction](client-cache-evidence.md).
