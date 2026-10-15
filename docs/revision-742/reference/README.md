# Research reference catalog — revision 742

These **41 feature references** contain period-preserved mechanics, numbers, recipes, unlocks, behavior and target-specific extraction work. Read together with the implementation guides. Their historical sources are **candidates**, not fully verified per-packet or per-cache parity evidence.

| Feature | Guide | Period research |
|---|---|---|
| agility | [Implementation](../agility.md) | [Research](./agility.md) |
| auras and customization | [Implementation](../auras-and-customization.md) | [Research](./auras-and-customization.md) |
| banking | [Implementation](../banking.md) | [Research](./banking.md) |
| clans | [Implementation](../clans.md) | [Research](./clans.md) |
| combat | [Implementation](../combat.md) | [Research](./combat.md) |
| construction | [Implementation](../construction.md) | [Research](./construction.md) |
| cooking | [Implementation](../cooking.md) | [Research](./cooking.md) |
| crafting | [Implementation](../crafting.md) | [Research](./crafting.md) |
| daily challenges | [Implementation](../daily-challenges.md) | [Research](./daily-challenges.md) |
| death | [Implementation](../death.md) | [Research](./death.md) |
| dueling | [Implementation](../dueling.md) | [Research](./dueling.md) |
| dungeoneering | [Implementation](../dungeoneering.md) | [Research](./dungeoneering.md) |
| equipment | [Implementation](../equipment.md) | [Research](./equipment.md) |
| farming | [Implementation](../farming.md) | [Research](./farming.md) |
| firemaking | [Implementation](../firemaking.md) | [Research](./firemaking.md) |
| fishing | [Implementation](../fishing.md) | [Research](./fishing.md) |
| fletching | [Implementation](../fletching.md) | [Research](./fletching.md) |
| herblore | [Implementation](../herblore.md) | [Research](./herblore.md) |
| hunter | [Implementation](../hunter.md) | [Research](./hunter.md) |
| inventory | [Implementation](../inventory.md) | [Research](./inventory.md) |
| magic | [Implementation](../magic.md) | [Research](./magic.md) |
| marketplace | [Implementation](../marketplace.md) | [Research](./marketplace.md) |
| minigames | [Implementation](../minigames.md) | [Research](./minigames.md) |
| mining | [Implementation](../mining.md) | [Research](./mining.md) |
| money pouch | [Implementation](../money-pouch.md) | [Research](./money-pouch.md) |
| prayer | [Implementation](../prayer.md) | [Research](./prayer.md) |
| progression | [Implementation](../progression.md) | [Research](./progression.md) |
| pvp | [Implementation](../pvp.md) | [Research](./pvp.md) |
| quests | [Implementation](../quests.md) | [Research](./quests.md) |
| runecrafting | [Implementation](../runecrafting.md) | [Research](./runecrafting.md) |
| runespan | [Implementation](../runespan.md) | [Research](./runespan.md) |
| shops | [Implementation](../shops.md) | [Research](./shops.md) |
| slayer | [Implementation](../slayer.md) | [Research](./slayer.md) |
| smithing | [Implementation](../smithing.md) | [Research](./smithing.md) |
| social | [Implementation](../social.md) | [Research](./social.md) |
| summoning | [Implementation](../summoning.md) | [Research](./summoning.md) |
| thieving | [Implementation](../thieving.md) | [Research](./thieving.md) |
| trading | [Implementation](../trading.md) | [Research](./trading.md) |
| travel | [Implementation](../travel.md) | [Research](./travel.md) |
| woodcutting | [Implementation](../woodcutting.md) | [Research](./woodcutting.md) |
| world and interactions | [Implementation](../world-and-interactions.md) | [Research](./world-and-interactions.md) |

## Machine-readable data

[Candidate skill recipe/catch tables and level-to-XP thresholds](../data/README.md). Eight historical datasets and one computed XP curve are included; missing IDs and source disagreements are intentionally visible instead of guessed.

## How to turn a candidate into authoritative data

1. Record exact local client/cache revision, binary hashes and build date. The 742 series received updates within November 2012, so a revision number is not a content fingerprint.
2. Compare each entry with cache definitions, relevant Hagalaz tables, and an interaction capture from the matching client.
3. Note the authoring evidence, date, method and expected field units; never infer IDs, tick counts or locations from historical narrative.
4. Only then mark a rule as verified and implement it with tests for exact values and cancelled/failed actions.
5. Update `docs/revision-742/research-evidence.md` if material corrections affect another feature.

## Exact client evidence

[Companion client and pinned cache extraction handoff](client-cache-evidence.md) records the discovered direct 742 fixture, index identities and code-to-cache workflow. Use it before promoting a wiki-derived number or a guessed item/animation/widget ID.
