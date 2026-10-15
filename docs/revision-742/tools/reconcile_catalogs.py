#!/usr/bin/env python3
"""Read-only revision 742 catalog reconciliation (standard library only)."""
import argparse
import csv
import hashlib
import json
from collections import Counter, defaultdict
from pathlib import Path


def rows(path, key):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        reader = csv.DictReader(stream, delimiter="\t" if path.suffix == ".tsv" else ",")
        if not reader.fieldnames or key not in reader.fieldnames:
            raise ValueError(f"{path}: missing {key} column")
        data = list(reader)
    for row in data:
        try:
            row[key] = int(row[key])
        except (ValueError, TypeError):
            raise ValueError(f"{path}: invalid {key}") from None
        if row[key] < 0:
            raise ValueError(f"{path}: negative {key}")
    return data


def indexed(data, key, label):
    output = {}
    for row in data:
        identifier = row[key]
        if identifier in output:
            raise ValueError(f"{label}: duplicate {key} {identifier}")
        output[identifier] = row
    return output


def sha256_file(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def cache_index(path, index, sha):
    source = rows(path, "id")
    result = indexed(source, "id", path)
    shift = 8 if index == 19 else 7
    for entry in source:
        identifier = entry["id"]
        if identifier > 65535:
            raise ValueError(f"{path}: identifier {identifier} exceeds current ushort DB key")
        if entry.get("fixture_sha256", "").lower() != sha:
            raise ValueError(f"{path}: missing or mismatched fixture_sha256 on {identifier}")
        if (int(entry.get("index", -1)) != index
                or int(entry.get("group", -1)) != identifier >> shift
                or int(entry.get("file", -1)) != identifier & ((1 << shift) - 1)):
            raise ValueError(f"{path}: invalid cache address on {identifier}")
        if not entry.get("raw_sha256"):
            raise ValueError(f"{path}: missing raw_sha256 on {identifier}")
    return result


def save(path, fields, data):
    with path.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=fields)
        writer.writeheader()
        writer.writerows(data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("fixture_archive", "cache_items", "cache_npcs", "db_items",
                 "db_npcs", "npc_spawns", "item_registrations", "npc_registrations", "out"):
        parser.add_argument("--" + name.replace("_", "-"), type=Path, required=True)
    parser.add_argument("--expected-sha256", required=True)
    args = parser.parse_args()

    sha = sha256_file(args.fixture_archive)
    if sha != args.expected_sha256.lower():
        raise ValueError("Approved fixture archive does not match expected SHA-256")

    cache_items = cache_index(args.cache_items, 19, sha)
    cache_npcs = cache_index(args.cache_npcs, 18, sha)
    db_items = indexed(rows(args.db_items, "id"), "id", args.db_items)
    db_npcs = indexed(rows(args.db_npcs, "npc_id"), "npc_id", args.db_npcs)
    spawns = indexed(rows(args.npc_spawns, "spawn_id"), "spawn_id", args.npc_spawns)

    scripts = {"item": defaultdict(set), "npc": defaultdict(set)}
    for domain, path in (("item", args.item_registrations), ("npc", args.npc_registrations)):
        for row in rows(path, "definition_id"):
            scripts[domain][row["definition_id"]].add(row.get("source_path", ""))

    problems = []
    def issue(level, domain, identifier, code, detail):
        problems.append(dict(level=level, domain=domain, id=identifier, code=code, detail=detail))

    spawn_counts = Counter()
    spawn_report = []
    fields = ("coord_x", "coord_y", "coord_z", "min_coord_x", "min_coord_y",
              "min_coord_z", "max_coord_x", "max_coord_y", "max_coord_z")
    for spawn_id, spawn in sorted(spawns.items()):
        try:
            npc_id = int(spawn["npc_id"])
            points = {field: int(spawn[field]) for field in fields}
        except (ValueError, KeyError, TypeError):
            issue("error", "spawn", spawn_id, "invalid_coordinates", "Missing or invalid NPC ID / coordinates")
            continue
        spawn_counts[npc_id] += 1
        notes = []
        if npc_id not in cache_npcs:
            notes.append("missing_npc_definition")
            issue("error", "spawn", spawn_id, "missing_npc_definition", str(npc_id))
        for axis in "xyz":
            low, center, high = (points[k + axis] for k in ("min_coord_", "coord_", "max_coord_"))
            if not low <= center <= high:
                notes.append("invalid_" + axis + "_bounds")
                issue("error", "spawn", spawn_id, "invalid_" + axis + "_bounds", f"{low}/{center}/{high}")
        if not 0 <= points["coord_z"] <= 3:
            notes.append("unusual_plane")
            issue("review", "spawn", spawn_id, "unusual_plane", str(points["coord_z"]))
        x, y = points["coord_x"], points["coord_y"]
        region = ((x >> 6) << 8 | y >> 6) if x >= 0 and y >= 0 else ""
        spawn_report.append(dict(spawn_id=spawn_id, npc_id=npc_id, region_id=region,
                                 **points, spawn_direction=spawn.get("spawn_direction", ""),
                                 status="review" if notes else "coordinates_valid", reasons=";".join(notes)))

    combined = {}
    for domain, definitions, database in (
        ("item", cache_items, db_items), ("npc", cache_npcs, db_npcs)):
        audit = []
        for identifier in sorted(set(definitions) | set(database) | set(scripts[domain])):
            cached, db = definitions.get(identifier), database.get(identifier)
            owners = scripts[domain].get(identifier, set())
            audit.append(dict(id=identifier, in_cache=bool(cached), in_database=bool(db),
                              registered_scripts=len(owners),
                              cache_name=cached.get("name", "") if cached else "",
                              database_name=db.get("name", "") if db else "",
                              group=cached.get("group", "") if cached else "",
                              file=cached.get("file", "") if cached else "",
                              raw_sha256=cached.get("raw_sha256", "") if cached else "",
                              npc_spawns=spawn_counts[identifier] if domain == "npc" else ""))
            if not cached:
                issue("error", domain, identifier, "missing_cache_record", "DB override or script references absent definition")
            if cached and db and cached.get("name") != db.get("name"):
                issue("review", domain, identifier, "display_name_differs", "Could be intentional server override")
            if len(owners) > 1:
                issue("review", domain, identifier, "duplicate_script_owners", ";".join(sorted(owners)))
        combined[domain] = audit

    args.out.mkdir(parents=True, exist_ok=True)
    columns = ["id", "in_cache", "in_database", "registered_scripts", "cache_name",
               "database_name", "group", "file", "raw_sha256", "npc_spawns"]
    save(args.out / "items.csv", columns, combined["item"])
    save(args.out / "npcs.csv", columns, combined["npc"])
    save(args.out / "npc-spawns.csv", ["spawn_id", "npc_id", "region_id", *fields,
                                     "spawn_direction", "status", "reasons"], spawn_report)
    problems.sort(key=lambda p: (p["domain"], str(p["id"]).zfill(12), p["code"]))
    save(args.out / "issues.csv", ["level", "domain", "id", "code", "detail"], problems)
    summary = dict(revision=742, fixture_sha256=sha,
                   cache_item_count=len(cache_items), cache_npc_count=len(cache_npcs),
                   database_item_count=len(db_items), database_npc_count=len(db_npcs),
                   npc_spawn_count=len(spawns), issue_counts=dict(sorted(Counter(
                       issue["code"] for issue in problems).items())))
    (args.out / "summary.json").write_text(json.dumps(summary, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(summary, sort_keys=True))


if __name__ == "__main__":
    main()
