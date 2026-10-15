# Magic and combat spells: complete input contract

## Target-compatible spell categories

Period spellbooks include standard utility/combat, ancient combat/teleports, and lunar support. [Ancient](https://wiki.darkan.org/Ancients) unlocks with Desert Treasure; switching at the desert pyramid altar drains Prayer to zero. Burst/Barrage casts can target an AoE around an entity's origin; Rush/Blitz are single-target. Miasmic requires a specific staff. [20 candidate spell rows](../data/researched/ancient-combat-spells.csv) include levels, base hits, base casting XP and effects, but **lack validated rune-cost images**. Do not infer rune IDs or quantities from unlabelled superscripts in the wiki.

## Existing Hagalaz owner and data join

- `Hagalaz.Services.GameWorld/Model/Creatures/Characters/Magic.cs`: owner of selected spell, rune check/consumption, infinite runes and autocast.
- `Hagalaz.Data/Entities/SkillsMagicCombatDefinition.cs`: button, required level, base damage/XP, serialized runes and rune counts, casting sequence/graphics, projectile and autocast configuration.
- `Hagalaz.Data/Entities/SkillsMagicTeleportDefinition.cs`: button/spellbook, target coordinates, level, rune count, XP and distance.
- `Hagalaz.Data/Entities/SkillsMagicEnchantDefinition.cs`: button, rune counts, level, animation/effect data and output join.
- `Hagalaz.Game.Scripts/Skills/Combat/Magic/` and `Hagalaz.Game.Scripts/Skills/Magic/`: existing spell-specific effect/teleport scripts.
- Client 742 index 3 components + index 12 scripts connect spell selection to outgoing game opcodes; resolve exact UI-to-server mapping instead of using spell names alone.

## Candidate ancient effect rules to verify

| Family | Success effect | Additional constraints |
|---|---|---|
| Smoke | poison target, initial 20 or 40 by spell tier | poison protection and resist/immune creatures |
| Shadow | temporary Attack reduction | source notes describe debuffs that do not reduce Attack indefinitely |
| Blood | restore one quarter of actual damage inflicted | clamps to caster current maximum health and handles zero hits |
| Ice | root for 5/10/15/20 seconds | movement action cancellation, immunity/re-root rules and tile reachability |
| Miasmic | slow melee/ranged attack rate by 50% for 12/24/36/48 seconds | staff requirement and special target restrictions; measured hit cap under staff bonus |

[Period spell table](https://wiki.darkan.org/Ancients). **Durations are real-world seconds in secondary prose; map them to the game's actual tick scheduler only after direct verification.**

## Authoritative cast flow

1. Server resolves spell identity from **selected 742 widget + verified spellbook** and the existing combat definition.
2. Validate caster state: alive, not blocked/stunned, spellbook unlocked, enough real/effective Magic, rune cost/staff substitutions, appropriate weapon/target and distance.
3. For target spells, evaluate live target ownership/plane, active cast policy and line of sight. For area spells, resolve targets using source-compatible tile sizes/instance/line of sight; never trust the client to list targets.
4. Compute hit and applicable target effects via existing combat owner; take a validated rune debit exactly once, award base/impact XP once, publish animation/projectile/effect. Handle failed cast and splash according to the specific historic effect.
5. Autocast stores selected spell identity and resets on incompatible equipment, book switching or inability to cast; don't create a separate autocast service.
6. Noncombat transforms (alchemy, enchant, teleport) must atomically move/replace item or coins with rune removal. Invalid noted/quest items and full stacks cannot lose resources.

## Deterministic cases

All spell tiers: insufficient rune(s), infinite-rune item, staff removal before cast, magic level drain, unreachable target behind wall, target movement during projectile, area with multiple NPC sizes, target logout/death, spellbook switch, cast interruption, item-on-item with stale slot, teleport to loaded/unloaded region and reconnect during cast.

**Missing** 742 rune-cost item IDs/counts for every spell; source spell list and button mapping for standard/lunar books; spell hit formula, projectile speed and render IDs; quest unlocks and special attack/defence interactions. Keep these fields null/unverified in a production ingestion draft.
