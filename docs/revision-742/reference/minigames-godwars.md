# God Wars — revision 742 research data

Part of [minigames](minigames.md).

The altar "Teleport" option lands the player on the faction's boss-room door tile. Placements decoded from the 742 cache (index 5, zero XTEA keys), plane 0:

| Faction | Altar object | Altar tile | Door object | Door tile (teleport destination) |
| --- | --- | --- | --- | --- |
| Armadyl | 26288 | (2818, 5301) | 26426 | (2835, 5294) |
| Bandos | 26289 | (2869, 5372) | 26425 | (2862, 5357) |
| Saradomin | 26287 | (2922, 5241) | 26427 | (2923, 5257) |
| Zamorak | 26286 | (2937, 5322) | 26428 | (2925, 5333) |

The previous values (Armadyl and Saradomin sharing one tile, Bandos and Zamorak sharing another, all on plane 2) matched no cache placement. The Saradomin door script (`Saradomin/BigDoor.rsobj.cs`) checks x=2907/2908, which disagrees with the cached door at x=2923 and is not yet investigated.
