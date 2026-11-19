# minigames — revision 742 research data

**Status:** period-reference candidate evidence; direct matching client/cache verification remains required.

## Historical behavior and implementation dataset

Barrows tunnels have a 3×3 room arrangement with a central chest, puzzle-gated central route and final brother encounter if not defeated earlier. Rare-loot chances in preserved sources are estimates, not reliable drop-rate constants. Source: https://wiki.darkan.org/Barrows. For each existing Hagalaz activity—Barrows, God Wars, Fight Caves, Soul Wars, Crucible, Duel Arena and Warrior Guild—capture instance/region geometry, entry, NPCs, waves, loot table, state transitions, death policy, exit/restart and UI config mappings independently.

## Barrows — pinned 742 cache evidence (issue #315)

**Source:** `Hagalaz.GameClient/tests/fixtures/cache742/cache.zip`, SHA-256 `2674f30b06bb7831fd51711b769e62208ae8236bde399f3fee1346ef03c891b1`, read through the existing `Hagalaz.Cache` codecs. Terms: **confirmed** = decoded from this cache; **interpretation** = inferred from confirmed data, not observed on a client; **unresolved** = not established.

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

### Barrows centre-door puzzle (issue #315)

Sources: the pinned 742 cache (renders of models 6713-6736 viewed locally, geometry decoded with the existing `ModelAssembler`), the interface and object definitions above, the [Luna Barrows issue](https://github.com/luna-rs/luna/issues/688) (a 377-cache implementation), the [RuneLite puzzle highlighter](https://github.com/runelite/runelite/pull/8552) (OSRS client), the [Darkan archive](https://wiki.darkan.org/Barrow) and the [barrows.fandom tunnels page](https://barrows.fandom.com/wiki/Tunnels). Terms as above; **cross-checked** = the table agrees across independent sources and at least partly with decoded 742 geometry.

#### Puzzle models (confirmed to exist and to be the puzzle shapes)

Models 6713-6736 all exist in the 742 cache and render as four families of six flat shapes: a triangle and a circle in rotating positions (6713-6718), a 2x2 grid with dark and light quadrants (6719-6724), a two-tone split with a moving boundary (6725-6730) and nested polygons (6731-6736). The same ids appear as the puzzle table in the 377 implementation and the same rule appears in RuneLite: the correct answer is the first sequence model minus 3.

| Puzzle | Sequence models (shown on components 6, 7, 8) | Choice models | Correct choice |
| --- | --- | --- | --- |
| Arrows (triangle and circle rotating) | 6716, 6717, 6718 | 6713, 6714, 6715 | 6713 |
| Squares (quadrants darkening) | 6722, 6723, 6724 | 6719, 6720, 6721 | 6719 |
| Bars (moving split) | 6728, 6729, 6730 | 6725, 6726, 6727 | 6725 |
| Polygons (shrinking sides) | 6734, 6735, 6736 | 6731, 6732, 6733 | 6731 |

How much of that is verified against the 742 geometry:

- **Squares: fully.** The sequence has no, one (+x+z), then two (+x) dark quadrants; the next quadrant added is -x-z, which leaves only -x+z light. That is 6719. The look-alike 6720 leaves -x-z light instead, and 6721 is all dark.
- **Arrows: partly.** The pair axis steps by -90 degrees through the sequence (6716 at 90, 6717 at 0, 6718 at -90), so the next is the 180 degree pose, which both 6713 and 6714 have; 6715 (0 degrees) is wrong. Telling 6713 from 6714 rests on the cited sources only.
- **Bars and polygons: not independently verified.** The renders are consistent with the cited table (bars: the light half flips to the -z side in 6725; polygons: sequence of shrinking polygons, choice 6731 is a four-sided shape), but the side counts of the polygon models could not be counted reliably because each is a ring with an inner polygon.

The correct choice is therefore the first model of the choice set; the answer position must be shuffled across the three buttons.

#### Interface 25 layout (confirmed from the cache)

Choice buttons are model components 2, 3 and 5 at x = -149, -6 and 137 (left to right, y 101), each with the single option "Choose". Sequence models are components 6, 7, 8 at x = -131, -38 and 55 inside container 4; fixed model 4623 (component 9, x 139) sits in the fourth slot, with labels "1." to "4." above them. Components 2, 3, 5, 6, 7, 8 start with model -1, so the server must set the models. Server scripts already do this with `IWidget.DrawModel(childId, modelId)` and register clicks with `IWidget.AttachClickHandler(childId, handler)`; centred interfaces are opened with `Widgets.OpenWidget(id, 0, script, interrupt)`. The click carries the component id and `Option1Click`. The components' camera fields (zoom and rotation) were not decoded here, and nothing was rendered on a live 742 client.

#### Doors (confirmed definitions, interpretation of locking)

- Every door slot is a varbit-driven transform. Slot `n` of config 452 bit `10 + n` belongs to the right-leaf object `6716 + n` (varbit `469 + n`) and to the left-leaf object `6735 + n`. The four chest-room doors are slots 4, 8, 9 and 11 (objects 6720, 6724, 6725, 6727 and variants 6739, 6743, 6744, 6746).
- Bit clear: object 6714 / 6733, name "Door", actions `Open` and `Examine`. Bit set: object 6715 / 6734, actions `Examine` only.
- **Interpretation:** a set bit hides the `Open` option, so locked doors show only `Examine`. This matches the period description that only the operational door has `Open Door` and the other three only `Examine`, but it was not observed on a live client. The door models are the same either way.
- Period behaviour (Darkan archive, barrows.fandom): one of the four chest-room doors is operable and carries the puzzle; the other three are locked; a wrong answer moves the catacombs around so that the rope and the chest-room door are in different rooms; the chest-room door is the only door that shows the puzzle. Monsters or a remaining brother can appear when a door is opened.

#### Still guesswork for the puzzle

- The success and failure chat messages, if any: none found in any source.
- Wrong answers (online search, later guides and wikis, not 742-specific): the OSRS Wiki and RuneHQ guides say the doors and rooms shift, the exit ladder can end up in a different room and the puzzle door becomes a different door; some RuneHQ pages say all doors re-randomize. The server therefore moves the operable door and the entry rope on a wrong answer and leaves other doors alone. No source mentions teleporting the player.
- What a wrong answer rerolls on the 742 server: the sources agree on entry room and chest-room door; Luna says only the chamber and door fields, Darkan says the catacombs move. The entry quadrant (config 452 bits 6-9) is the one client-visible part.
- Whether the perimeter doors (slots 0, 1, 7 and 15) are permanently locked: they sit on the outer boundary of the tunnel, so a set bit is the likely state, but no source states it.
- The exact number of ticks or sound used when a door opens (Luna: sound 1277 and 1278 from the 377 client; not a 742 fact).
- The 742 client behaviour when the interface is closed without an answer.

### Still unresolved

HUD CS2 semantics; whether the 742 server rerolls the whole tunnel or only the doors on a wrong answer (see the puzzle section below); success/failure chat messages; entry-spawn tile walkability; NPC combat stats, specials and proc rates; prayer-drain interval (10s vs 18s); chest loot rows (database, not cache); quest-completion persistence.

## Acceptance evidence still missing

- Direct 742 client/cache ID and protocol captures with checksum and date.
- Complete content rows and the matching Hagalaz repository definitions.
- Positive, negative, interruption, concurrency and persistence tests for each interaction.

[Implementation guide](../minigames.md) · [Research policy](../research-evidence.md)

### Barrows chest 10284: live loot rows (confirmed from the local Aspire MySQL database, 2026-11-19)

`gameobject_definitions`: object 10284 "Barrows Chest" uses `gameobject_loot_id = 8`.

- **Table 8 `barrows_chest`:** `maximum_loot_count = 2`, `randomize_loot_count = 1`; child table 9 via `gameobject_lootrecursion`. 37 item rows, total weight 539.
  - Two "nothing" rows (item 0) at weights 250 and 25.
  - Coins 995 (1-3931, w 50), mind runes 558 (250-4900, w 45), chaos 562 (115-1890, w 40), death 560 (70-1190, w 30), blood 565 (35-630, w 25), bolt racks 4740 (35-280, w 15).
  - Item 7462 at weight 10.
  - 28 armour pieces at weight 1.75 each: the six original sets (4708-4759 range) **and the four Akrisae pieces 21736, 21744, 21752, 21760**.
- **Child table 9 `barrows_chest_rare`:** `maximum_loot_count = 1`; runes 558 x60 (w 100), 560 x15 (w 90), items 165/141/129 (w 50), 385 x4 (w 50), 4740 (w 50), key halves 985 and 987 (w 2 each), dragon medium helm 1149 (w 2).

Consequences (confirmed from the rows, not from play-testing):

- The chest roll takes **no input** from slain brothers or crypt creatures, so kills never change the odds. The one per-player difference is the ring of wealth: `LootStore` adds `RingOfWealthModifier` to tables whose name contains "rare" (here `barrows_chest_rare`, table 9), which can raise individual weights for players wearing the ring.
- **Akrisae's set can drop for players who have not completed the quest.**
- Item 7462 is not a period Barrows chest reward in any source reviewed; its origin here is unresolved.

#### Chest armour change (seeded through `BarrowsLootSeedSource`, no EF migration and no SQL)

Table 8 is no longer rolled and is left as it is in every database. Tables 36-41 (`barrows_set_*`, one piece per roll, equal weight) hold each standard brother's four pieces, and `BarrowsScript` rolls them only for brothers slain in the run: `min(N+1, 7)` rolls, each a hit at 1 in `max(450 - 58N, 73)` (`BarrowsRewards`). These rates come from the later published model and are **not verified for 742**; they are the single place to tune. Akrisae's pieces are no longer in any table while his encounter is locked. Names must not contain "rare", or the ring of wealth modifier is applied. Ordinary rewards are described in the next section.

#### Akrisae combat (implemented in `AkrisaeTheDoomed`)

The database row for NPC 14297 (checked through the local Aspire MySQL, 2026-11-19) has 1,050 life points (the ×10 scale), attack speed 8, attack animation 422 and magic level 70 / attack bonuses favouring magic. The script now attacks with melee (period sources: Darkan archive, Tip.It) using the standard NPC melee damage, and keeps a small protection state, not an NPC prayer system: after 150 damage from one style he protects against it, that style's hits are reduced to 60%, and he skips his next attack. 150 is the pre-EoC figure; with 1,050 life points the post-EoC 1,500 could never be reached. The 60% reduction is the tormented demon's convention; no period source gives Akrisae's own value. The existing prayer drain on hit is kept but now applies when the player is not using Protect from Melee. Proc chances for transferring or disabling prayers are not established and were not added.

#### Ahrim's Blighted Aura (implemented in `AhrimTheBlighted`)

A successful magic hit lowers the target's Strength by 5 levels with a 20% chance. The 20% is the Darkan archive's "approximately 1 out of 5 casts"; other sources give 1/4 (an older guide), 1/8 (a newer one) and 20-25% (OSRS), so the value is a documented default, not a 742 measurement. No chat message is sent because no source gives one.

#### Ordinary rewards: reward potential (replaces rolling table 8)

Each claim gives `min(N+1, 7)` rewards for N slain brothers. Each reward is either that brother-armour hit (above) or an ordinary roll: a random number in [0, potential] picks a band, where potential = min(sum of slain crypt-creature combat levels, 1000) + 2 x N, so at most 1,014. Bands: 0-379 coins, 380-504 mind runes, 505-629 chaos, 630-754 death, 755-879 blood, 880-1004 Karil's bolt racks, 1005-1007 loop half, 1008-1010 tooth half, 1011+ dragon medium helm. This is the published reward-potential model (the Jagex drop-rate explanation, which also describes Linza, Rise of the Six tokens and defenders; those later additions are not implemented, and neither is the ring of wealth bonus drop). It is the closest-era source: the 742 cache's config 453 holds a 10-bit field (varbit 463) that fits the 1,014 maximum, but the field's use and the exact thresholds are not measured on 742. Crypt-creature combat levels come from the cache (2031 Bloodworm 52, 2032 Crypt rat 43, 2033 Giant crypt rat 76, 2034 Crypt spider 56, 2035 Giant crypt spider 79, 2036 and 2037 Skeleton 77); the database has no rows for those NPCs.

The bands are game-object loot tables 42-50 (`barrows_reward_*`, one item each at the old table 8 counts: coins 1-3931, mind 250-4900, chaos 115-1890, death 70-1190, blood 35-630, bolt racks 35-280, keys 985 and 987, dragon medium helm 1149), seeded like tables 36-41. `LootChest` no longer rolls table 8 or its child table 9. Table 9's extra items (three potions, 4 sharks, runes) therefore no longer drop; they resemble the later ring of wealth bonus drop, which is left out. A claim stays unclaimed if any chosen table is missing.

#### Seeding game-world data (`Hagalaz.Database.Migrations`)

The migrations project runs the registered `IGameWorldSeeder`s (`GameObjectLootSeeder`, fed by `BarrowsLootSeedSource`) through `MigrateDatabase`'s initialization hook, under the migration lock and after the schema is current, so every service (which waits for that project) starts with the tables present. The rule is per table id and never overwrites: a table is created, with its items, only when its id is absent from `gameobject_loot`. An existing table, edited or not, is left untouched; an id that is used under a different name is left untouched and logged as a warning (the chest may then not be claimable). Tables 36-50 are the only ids it owns; table 8 and every other table are never read or changed. The behaviour is covered by integration tests that run the seeder against a real MySQL container (empty database, a second run, an edited table, a foreign table on an owned id, and an unrelated table). Existing deployments get the missing tables on their next start; fresh databases get all of them.