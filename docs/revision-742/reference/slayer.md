# slayer — revision 742 research data

**Evidence tier:** period-preserved secondary reference, pending target build verification.

## Task reward-point table (post-unlock candidate)

| Master | Normal task | 10th task | 50th task |
|---|---:|---:|---:|
| Mazchna / Achtryn | 2 | 5 | 15 |
| Vannaka | 4 | 20 | 60 |
| Chaeldar | 10 | 50 | 150 |
| Sumona | 12 | 60 | 180 |
| Duradel / Lapalok | 15 | 75 | 225 |
| Kuradal | 18 | 90 | 270 |

Slayer points begin only after qualifying tasks following **Smoking Kills**; the first four count towards the streak but do not pay points. Turael/Spria assignments do not reward points. Multipliers are **5×** on each tenth and **15×** on each fiftieth, after eligibility. Sources: [Slayer points](https://wiki.darkan.org/Slayer_points), [Slayer masters](https://wiki.darkan.org/Slayer_Master).

**To implement:** fill missing point logic in the existing `Slayer.cs`, keyed by assigned master identity, kill family and completed streak. Test task 1–5, 10, 50, master switches, cancellation and quest unlock. Monster tables and weighting still need independent 742 confirmation.

## Remaining target evidence

Validate all recipe, NPC/object, animation and config IDs in a captured 742 client/cache; trace live click packets, skill XP changes and cancellation through existing Hagalaz handlers. Numeric rows are candidates, not yet authoritative server constants.

[Implementation guide](../slayer.md) · [Research policy](../research-evidence.md)

## Expanded research materials

- [Master point data](../data/researched/slayer-master-points.csv)
- [Reward shop](../data/researched/slayer-point-rewards.csv)
- [Progress/reward integrity](deep-slayer.md)

These additions include period-source candidate facts or checked Hagalaz code paths. They are **not a completed 742 client/cache parity capture**.
