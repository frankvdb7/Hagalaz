# Revision 742: Fletching recipe crosswalk and Runecrafting essence eligibility

**Status:** Source-backed runtime definitions, not verified against the approved client cache. This dossier complements [production skill behavior](production-skill-source-audit.md), [the canonical recipe candidates](data/researched/README.md) and [the skill regression matrix](skills-reconciliation-test-matrix.md). Do not treat script IDs as independently verified client identities.

## Fletching: all literal source definitions extracted

[Normalized Fletching source recipes](data/catalogs/fletching-source-recipes.csv) contains **90 output-variant rows** from **78 static `FletchingDefinition` instances** in `Hagalaz.Game.Scripts/Skills/Fletching/FletchingSkillService.cs`.

| Source array | Definition count | Output variant count | Meaning |
| --- | ---: | ---: | --- |
| `Wood` | 6 | 18 | Logs combined with tool to make multiple unstrung products |
| `Bows` | 26 | 26 | Bow/crossbow assembly and stringing variants |
| `Ammo` | 35 | 35 | Arrows, bolts, darts, grapple and other ammo products |
| `Tips` | 11 | 11 | Gem/tool use to make bolt tips |
| **Total** | **78** | **90** | Every literal entry in these four source arrays |

Each CSV row includes the *source array name*, resource and tool item IDs, target product item ID, configured product quantity (defaulting to 1 when the source constructor omits that array), required skill level, configured XP value, animation ID (default `-1`), file path, source line and evidence status `repository_source_unverified_742`.

These are declarations, not a guarantee that every recipe can currently be invoked. Confirm factory registration, item scripts, menu selection, achievement/quest checks, inventory contracts and response packets. Before changing output amounts or rates, compare with `data/fletching-bows.csv` and the `data/researched/fletching-*.csv` historical candidate files; maintain distinctions between per-action and per-item XP.

### Example source relations

| Source item | Tool item | Product item | Product count | Required level | Configured XP |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1511 | 946 | 52 | 15 | 1 | 0.33 |
| 1511 | 946 | 50 | 1 | 5 | 5.0 |
| 50 | 1777 | 841 | 1 | 5 | 5.0 |
| 52 | 314 | 53 | 15 | 1 | 1.0 |
| 39 | 53 | 882 | 15 | 1 | 2.6 |

The table is illustrative; the CSV is the complete normalized extraction of these source arrays. The values have **not** been checked against cache image/models or produced as a runtime export.

## Runecrafting: when both essence types are present

`Hagalaz.Game.Scripts/Skills/Runecrafting/Altar.rsobj.cs` retrieves the altar definition but then prefers ordinary essence if either type is in inventory. Only afterwards does it reject ordinary essence for definitions requiring level 27 or above, even when pure essence is available.

This conflicts with [historical eligibility evidence](https://wiki.darkan.org/Runecrafting): ordinary essence is usable for the basic rune set while pure essence is eligible for the broader set. It does *not* mean every rune can be crafted from ordinary essence just because a mixed inventory exists.

| Altar requirement | Inventory | Current code | Correct eligibility expectation |
| --- | --- | --- | --- |
| Pure-only level 27+ | Pure only | Uses pure | Allowed |
| Pure-only level 27+ | Ordinary only | Rejects | Reject |
| Pure-only level 27+ | Ordinary **and** pure | **Rejects** due to ordinary-first selection | **Use pure**; ordinary remains |
| Basic | Ordinary only | Uses ordinary | Allowed |
| Basic | Pure only | Uses pure | Allowed |
| Basic | Both | Uses ordinary | Check historical preference; do not consume both |

The issue and regression plan are [#527](https://github.com/frankvdb7/Hagalaz/issues/527). For each case also verify XP = configured XP × *actual essence removed* and product quantity = selected output multiplier × actual essence removed. The currently ignored `Inventory.Items.Add(runes)` boolean does not by itself prove an ordinary full-inventory bug: consuming unstackable essence typically frees space first. Check integer-overflow/stacking contract independently if a concrete report requires it.

## Next verification steps

1. Extract supported **server database** Runecrafting altar rows and original 742 client object/item IDs; the committed CSVs are scripts, not DB snapshots.
2. Confirm all Fletching definitions are registered by the actual factories and identify unused/duplicate entries.
3. Compare output XP and quantities to historical candidate datasets with explicit per-field confidence; do not overwrite contested values.
4. Create targeted script tests for input selection, per-item capacity and cancellation using existing inventory boundaries; keep runtime fixes out of this documentation-only PR.
