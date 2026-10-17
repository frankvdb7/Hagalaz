-- Database-only checks that can run before a cache export is available.
-- Read-only SQL; no temporary tables, migrations, credentials or output file writes.
-- Counts describe only the selected database snapshot, not the full client cache.

SELECT COUNT(*) AS item_definition_rows FROM item_definitions;
SELECT COUNT(*) AS npc_definition_rows FROM npc_definitions;
SELECT COUNT(*) AS npc_spawn_rows FROM npc_spawns;
SELECT COUNT(DISTINCT npc_id) AS distinct_spawned_npc_ids FROM npc_spawns;

SELECT s.spawn_id, s.npc_id, s.coord_x, s.coord_y, s.coord_z
FROM npc_spawns AS s
LEFT JOIN npc_definitions AS n ON n.npc_id = s.npc_id
WHERE n.npc_id IS NULL
ORDER BY s.spawn_id;

SELECT spawn_id, npc_id, coord_x, coord_y, coord_z,
       min_coord_x, min_coord_y, min_coord_z,
       max_coord_x, max_coord_y, max_coord_z
FROM npc_spawns
WHERE coord_x NOT BETWEEN min_coord_x AND max_coord_x
   OR coord_y NOT BETWEEN min_coord_y AND max_coord_y
   OR coord_z NOT BETWEEN min_coord_z AND max_coord_z
   OR min_coord_x > max_coord_x
   OR min_coord_y > max_coord_y
   OR min_coord_z > max_coord_z
ORDER BY spawn_id;

SELECT s.npc_id, s.attack_level, s.defence_level, s.strength_level
FROM npc_statistics AS s
LEFT JOIN npc_definitions AS d ON d.npc_id = s.npc_id
WHERE d.npc_id IS NULL
ORDER BY s.npc_id;

SELECT b.npc_id
FROM npc_bonuses AS b
LEFT JOIN npc_definitions AS d ON d.npc_id = b.npc_id
WHERE d.npc_id IS NULL
ORDER BY b.npc_id;

SELECT n.npc_id, n.npc_loot_id
FROM npc_definitions AS n
LEFT JOIN npc_loot AS l ON l.id = n.npc_loot_id
WHERE n.npc_loot_id IS NOT NULL AND l.id IS NULL
ORDER BY n.npc_id;

SELECT n.npc_id, n.npc_pickpocketing_loot_id
FROM npc_definitions AS n
LEFT JOIN npc_loot AS l ON l.id = n.npc_pickpocketing_loot_id
WHERE n.npc_pickpocketing_loot_id IS NOT NULL AND l.id IS NULL
ORDER BY n.npc_id;

SELECT d.npc_id, d.name AS npc_name, f.spot_id
FROM skills_fishing_spot_npc_definitions AS f
LEFT JOIN npc_definitions AS d ON d.npc_id = f.npc_id
WHERE d.npc_id IS NULL
ORDER BY f.npc_id, f.spot_id;

SELECT m.npc_id
FROM skills_slayer_master_definitions AS m
LEFT JOIN npc_definitions AS d ON d.npc_id = m.npc_id
WHERE d.npc_id IS NULL
ORDER BY m.npc_id;
