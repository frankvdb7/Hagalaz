-- Approved read-only SELECTs. Execute each query separately; export headers as TSV.
-- Schema checked against Hagalaz.Data/HagalazDbContext.cs.
-- Do not use INTO OUTFILE or commit private database exports.

-- db-items.tsv
SELECT id, name, examine, trade_price, low_alchemy_value, high_alchemy_value, tradeable, weight
FROM item_definitions ORDER BY id;

-- db-npcs.tsv
SELECT npc_id, name, examine, respawn_time, combat_level, reaction_type, bounds_type,
       attackable, walks_randomly, attack_speed, attack_animation, attack_graphic,
       defence_animation, defence_graphic, death_animation, death_graphic, death_ticks,
       npc_loot_id, npc_pickpocketing_loot_id, slayer_level_required
FROM npc_definitions ORDER BY npc_id;

-- npc-spawns.tsv
SELECT spawn_id, npc_id, coord_x, coord_y, coord_z, min_coord_x, min_coord_y,
       min_coord_z, max_coord_x, max_coord_y, max_coord_z, spawn_direction
FROM npc_spawns ORDER BY spawn_id;

-- npc-statistics.tsv
SELECT npc_id, max_lifepoints, attack_level, defence_level, strength_level, ranged_level, magic_level
FROM npc_statistics ORDER BY npc_id;

-- npc-bonuses.tsv
SELECT * FROM npc_bonuses ORDER BY npc_id;

-- npc-loot.tsv
SELECT id, name, maximum_loot_count, randomize_loot_count, always
FROM npc_loot ORDER BY id;

-- npc-loot-items.tsv
SELECT id, npc_loot_id, item_id, minimum_count, maximum_count, probability, always
FROM npc_loot_items ORDER BY id;

-- fishing-spot-npcs.tsv
SELECT spot_id, npc_id FROM skills_fishing_spot_npc_definitions ORDER BY spot_id, npc_id;

-- slayer-masters.tsv
SELECT npc_id, name, base_slayer_reward_points
FROM skills_slayer_master_definitions ORDER BY npc_id;
