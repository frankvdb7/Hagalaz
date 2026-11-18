# minigames — revision 742 research data

**Status:** period-reference candidate evidence; direct matching client/cache verification remains required.

## Historical behavior and implementation dataset

Barrows tunnels have a 3×3 room arrangement with a central chest, puzzle-gated central route and final brother encounter if not defeated earlier. Rare-loot chances in preserved sources are estimates, not reliable drop-rate constants. Source: https://wiki.darkan.org/Barrows. For each existing Hagalaz activity—Barrows, God Wars, Fight Caves, Soul Wars, Crucible, Duel Arena and Warrior Guild—capture instance/region geometry, entry, NPCs, waves, loot table, state transitions, death policy, exit/restart and UI config mappings independently.

## Barrows — pinned 742 cache evidence (issue #315)

**Source:** `Hagalaz.GameClient/tests/fixtures/cache742/cache.zip`, SHA-256 `2674f30b06bb7831fd51711b769e62208ae8236bde399f3fee1346ef03c891b1`, read through the existing `Hagalaz.Cache` codecs. Reproduce with `Hagalaz.Cache.Tests/Barrows742EvidenceTests.cs`: extract the zip, set `HAGALAZ_CACHE742_PATH` to that directory (optionally `BARROWS_EVIDENCE_REPORT` for the raw report) and run `dotnet test Hagalaz.Cache.Tests --filter Barrows742`. Without the variable the tests pass trivially. Terms: **confirmed** = decoded from this cache; **interpretation** = inferred from confirmed data, not observed on a client; **unresolved** = not established.

### Varbits and configs (confirmed)

`VarpBitDefinition.BitLength` holds the *most-significant bit index*, not a width.

| Config | Bits | Varbit | Meaning |
| --- | --- | --- | --- |
| 453 | 0–5 | 457–462 | six brother flags (one bit each) |
| 453 | 6–15 | 463 | 10-bit field (named "reward potential" only in a later client; use unresolved) |
| 453 | 16 | 1394 | single bit; **also the varbit of chest object 10284** (transform `[6774, 6775, -1]`: 0 → closed, 1 → open) |
| 453 | 17–31 | 464 | 15-bit field (creatures slain count) |
| 452 | 6–9 | 465–468 | one bit per entry rope: objects 6709–6712 (transform `[-1, 6708, -1]`: bit set shows the climbable rope 6708) |
| 452 | 10–25 | 469–484 | one bit per door slot: objects 6716–6731 and 6735–6750 (transform `[6714/6733, 6715/6734, -1]`) |

There is **no seventh (Akrisae) brother-flag varbit in config 453**. Bits 0–5 are the six brothers; bit 6 is the lowest bit of varbit 463.

### Tunnel objects (confirmed from map placements, region 14231 = x 55 / y 151, decoded with zero XTEA keys)

- **Ropes:** 6709 (3534,9712) NW, 6710 (3568,9712) NE, 6711 (3534,9678) SW, 6712 (3568,9678) SE, all placed at plane 0.
- **Chest 10284** at (3551,9695), the centre of the 3×3 arrangement.
- **Doors:** every one of the 32 ids 6716–6731 and 6735–6750 is placed exactly twice. Ids 6716, 6717, 6723, 6731 (and variants 6735, 6736, 6742, 6750) sit on the outer boundary (x 3528/3575, y 9671/9718). The 24 inner ids 6718–6722, 6724–6730 and 6737–6741, 6743–6749 are the doors between rooms. The four doors around the chest are 6720, 6724, 6725, 6727 (variants 6739, 6743, 6744, 6746), at (3551,9701)/(3552,9705), (3541,9695)/(3545,9694), (3558,9695)/(3562,9694) and (3551,9684)/(3552,9688).
- Door state (interpretation): the clickable "Open" door is the varbit-0 transform (6714/6733); the varbit-1 transform (6715/6734) has no actions. So a set bit in config 452 bits 10–25 leaves that door slot non-interactive.

### Interfaces (confirmed structure; models and CS2 semantics unresolved)

Decoded with a port of the client's `parseIComponentDefinition` (no parse errors or trailing bytes).

- **24 (kill count HUD), 15 components:** text 3 ("None", brothers-slain list), 4 ("Brothers slain:"), 6 ("0", creature count), 7 ("Creatures slain:"), seven empty model components 8–14. Component 0 has `configTriggers=[453]`, `configChanged=[1394]`, `cs2IntTriggers=[1043]`, `cs2IntChanged=[1715]`; components 3, 4 and 6 also react to config 453 / 1394. The HUD is therefore driven client-side from config 453 and varbit 1394. What scripts 1394/1715/1043 compute is **unresolved**.
- **25 (puzzle), 15 components:** question text 1 ("Which is the next shape in the above sequence?"), three choice buttons 2, 3, 5 (model components with the "Choose" option), three sequence model components 6, 7, 8 inside container 4, a fixed model 4623 in component 9, labels 11–14 ("1."–"4."). **Interface 25 carries no callbacks or triggers**: the sequence/answer models and answer handling are server-driven. The shape model ids for the four puzzles are **unresolved**.

### NPCs (confirmed)

Names and combat levels only (the cache holds no attack stats): 2025 Ahrim the Blighted 130, 2026 Dharok the Wretched 196, 2027 Guthan the Infested 147, 2028 Karil the Tainted 121, 2029 Torag the Corrupted 146, 2030 Verac the Defiled 146, 14297 Akrisae the Doomed 115. Crypt creatures: 2031 Bloodworm 52, 2032 Crypt rat 43, 2033 Giant crypt rat 76, 2034 Crypt spider 56, 2035 Giant crypt spider 79, 2036 and 2037 Skeleton 77.

### Other objects (confirmed)

Sarcophagi 61189, 63177, 66016–66020 are all "Sarcophagus" with a "Search" option; 6702–6707 are "Staircase" with "Climb-up"; 66115/66116 "Spade(s)" with "Dig-with".

### Quest (confirmed)

"Ritual of the Mahjarrat" is cache quest id **194** (not the wiki's 176). Its only progress source is **varbit 9829**: started at ≥ 10, completed at ≥ 400; no progress varps. The varbit's parent config was not extracted. Whether a live character stores this varbit is **unresolved** (see issue #315, section B).

### Consequences for `Hagalaz.Game.Scripts/Minigames/Barrows`

| Code | Evidence |
| --- | --- |
| `(1 + _cryptStartIndex) << 6` (64/128/192/256) | **Wrong.** Each rope has its own bit: the value must be `1 << (6 + index)` = 64/128/256/512, matching the comments in `BarrowsConstants`. 192 sets two rope bits and 256 shows the third rope for the fourth entry. |
| `CryptRope` registers 6709–6711 | **Missing 6712**, the south-east rope (entry index 3), which is placed and varbit-driven like the others. |
| Door ids 6718–6730 (not 6723), 6737–6749 (not 6742) | **Confirmed**: exactly the 24 inner doors; the excluded ids are perimeter doors. |
| `DrawAllKills` chest on bit 16, count on bit 17 | **Confirmed** (varbits 1394 and 464). |
| `DrawAllKills` sets bit `i` for all seven brothers | Bit 6 (Akrisae) is the low bit of the 10-bit varbit 463, so it should not be written as a brother flag; where the client shows Akrisae is unresolved. |
| `SendBitConfiguration(464, kills)` | Consistent with varbit 464 (bits 17–31 of config 453). |

### Still unresolved

Shape models and the answer/reroll protocol for the four puzzles; HUD CS2 semantics; which door bit value locks a door; entry-spawn tile walkability; NPC combat stats, specials and proc rates; prayer-drain interval (10s vs 18s); chest loot rows (database, not cache); quest-completion persistence.

## Acceptance evidence still missing

- Direct 742 client/cache ID and protocol captures with checksum and date.
- Complete content rows and the matching Hagalaz repository definitions.
- Positive, negative, interruption, concurrency and persistence tests for each interaction.

[Implementation guide](../minigames.md) · [Research policy](../research-evidence.md)
