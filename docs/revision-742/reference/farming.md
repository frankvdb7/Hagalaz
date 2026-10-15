# farming — revision 742 research data

**Evidence tier:** period-preserved secondary reference, pending target build verification.

## Candidate patch growth timings

| Crop/patch | Level | Time structure | Plant XP | Harvest XP shown |
|---|---:|---|---:|---:|
| Potatoes (allotment) | 1 | 4 × 10 min | 8 | 9 per harvest |
| Onions (allotment) | 5 | 4 × 10 min | 9.5 | 10.5 per harvest |
| Marigolds (flower) | 2 | 4 × 5 min | 8.5 | 47 |
| Rosemary (flower) | 11 | 4 × 5 min | 12 | 66.5 |
| Limpwurt (flower) | 26 | 5 × 5 min | 21.5 | 120 |

Preserved Farming tables also include yield, disease, compost, farmer protection payment, harvesting items and weed lifecycle. Source: [Farming](https://wiki.darkan.org/Farming).

**To implement:** save crop type, patch ID, timestamp, growth stage, compost state, health/disease, protection, harvested yield and per-cycle payout. Use elapsed-time advancement on server instead of one background timer for each patch. Require object identity and seed mapping from 742 cache.

## Remaining target evidence

Validate all recipe, NPC/object, animation and config IDs in a captured 742 client/cache; trace live click packets, skill XP changes and cancellation through existing Hagalaz handlers. Numeric rows are candidates, not yet authoritative server constants.

[Implementation guide](../farming.md) · [Research policy](../research-evidence.md)

## Expanded research materials

- [Patch lifecycle](deep-farming.md)
- [Seed and patch EF model](repository-data-crosswalk.md)

These additions include period-source candidate facts or checked Hagalaz code paths. They are **not a completed 742 client/cache parity capture**.

[13 candidate allotment/flower rows](../data/researched/farming-allotments-and-flowers.csv) · [Growth and offline persistence analysis](deep-farming.md)
