# fishing — revision 742 research data

**Evidence tier:** period-preserved secondary reference. Numeric entries below are candidate parity fixtures, **not yet verified against a 742 cache**.

## Historical basic catch table

| Catch | Level | Equipment | XP per catch |
|---|---:|---|---:|
| Crayfish | 1 | Crayfish cage | 10 |
| Shrimps | 1 | Small net | 10 |
| Tuna | 35 | Harpoon | 80 |
| Lobster | 40 | Lobster pot | 90 |

These are examples, not a complete table: trout/salmon, swordfish, shark, monkfish, cave fish, rocktail, bait use, and spot relocation must be separately extracted. Sources: [free fishing progression](https://wiki.darkan.org/Free-to-play_Fishing_training), [raw lobster](https://wiki.darkan.org/Raw_lobster).

**To implement:** map every target fishing NPC definition to click option -> fishing method -> required tool/bait -> weighted catch entries -> XP -> move/respawn behavior. A successful catch consumes bait only for methods requiring bait. Verify catch rates and NPC IDs with 742 captures.

## Repository baited-method definition used by bait tests

Hagalaz seed definition for the fly-fishing method (database-backed `FishingService.FindSpotByNpcIdClickType` source; extracted from the repository seed dump, **not** a 742 cache capture):

| Spot | NPC IDs | Click | Tool | Bait | Catch (item, level, XP, weight) |
|---:|---|---|---|---|---|
| 1 | 309, 329 | Option1Click | 309 (fly fishing rod, anim 623/622) | 314 (feather) | 331 (30, 70.0, 2.0); 335 (20, 50.0, 3.0) |

`FishingBaitTests` pins these IDs and XP values and verifies one bait per **delivered** fish, a single bait never funding two fish, and no bait or XP for a rejected insertion. Cost policy source: [2011.rs fishing basics](https://2011.rs/kb/fishing_the_basics), [fishing bait](https://wiki.darkan.org/Fishing_bait). The spot IDs and weights still need 742 cache/capture verification.

## Implementation evidence still required

- Target client/cache item, NPC, object, animation, config and interface identifiers, with provenance and hash.
- Relevant Hagalaz database row/definition mapping and focused tests.
- Capture of failure, interruption, concurrency, full-container and login/logout behavior.

[Implementation guide](../fishing.md) · [Research policy](../research-evidence.md)

## Expanded research materials

- [Special fishing methods](../data/researched/barbarian-fishing.csv)
- [Familiar fishing bonus](../data/researched/fishing-familiar-bonuses.csv)
- [Task and catch ownership](deep-gathering.md)

These additions include period-source candidate facts or checked Hagalaz code paths. They are **not a completed 742 client/cache parity capture**.
