#!/usr/bin/env python3
"""Regression tests for the read-only 742 catalog comparer; entirely synthetic."""
import csv
import hashlib
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


class ReconcileTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.path = Path(self.temp.name)
        archive = self.path / "cache.zip"
        archive.write_bytes(b"synthetic-fixture-not-a-real-cache")
        self.sha = hashlib.sha256(archive.read_bytes()).hexdigest()
        self.archive = archive
        self.write("item.csv", ["id", "index", "group", "file", "name", "fixture_sha256", "raw_sha256"],
                   [[995, 19, 3, 227, "Coins", self.sha, "abc"]])
        self.write("npc.csv", ["id", "index", "group", "file", "name", "fixture_sha256", "raw_sha256"],
                   [[5362, 18, 41, 114, "Dragon", self.sha, "def"]])
        self.write("item-db.tsv", ["id", "name"], [[995, "Coins"]], delimiter="\t")
        self.write("npc-db.tsv", ["npc_id", "name"], [[5362, "Dragon"]], delimiter="\t")
        self.write("spawn.tsv", ["spawn_id", "npc_id", "coord_x", "coord_y", "coord_z",
                                "min_coord_x", "min_coord_y", "min_coord_z",
                                "max_coord_x", "max_coord_y", "max_coord_z"],
                   [[1, 5362, 3222, 3223, 0, 3220, 3220, 0, 3225, 3225, 0]], delimiter="\t")
        self.write("item-registrations.csv", ["definition_id", "source_path"], [[995, "Coins.cs"]])
        self.write("npc-registrations.csv", ["definition_id", "source_path"],
                   [[5362, "Dragon.cs"], [5362, "BrutalDragon.cs"]])

    def write(self, name, headings, data, delimiter=","):
        with (self.path / name).open("w", encoding="utf-8", newline="") as handle:
            writer = csv.writer(handle, delimiter=delimiter)
            writer.writerow(headings)
            writer.writerows(data)

    def run_cli(self, sha=None):
        return subprocess.run([
            sys.executable, str(Path(__file__).with_name("reconcile_catalogs.py")),
            "--fixture-archive", str(self.archive), "--expected-sha256", sha or self.sha,
            "--cache-items", str(self.path / "item.csv"),
            "--cache-npcs", str(self.path / "npc.csv"),
            "--db-items", str(self.path / "item-db.tsv"),
            "--db-npcs", str(self.path / "npc-db.tsv"),
            "--npc-spawns", str(self.path / "spawn.tsv"),
            "--item-registrations", str(self.path / "item-registrations.csv"),
            "--npc-registrations", str(self.path / "npc-registrations.csv"),
            "--out", str(self.path / "results"),
        ], capture_output=True, text=True)

    def test_reconciles_and_reports_overlap(self):
        result = self.run_cli()
        self.assertEqual(result.returncode, 0, result.stderr)
        summary = json.loads((self.path / "results" / "summary.json").read_text())
        self.assertEqual(summary["npc_spawn_count"], 1)
        self.assertEqual(summary["cache_item_count"], 1)
        self.assertEqual(summary["issue_counts"]["duplicate_script_owners"], 1)
        with (self.path / "results" / "npc-spawns.csv").open(newline="") as handle:
            spawn = next(csv.DictReader(handle))
        self.assertEqual(spawn["region_id"], str((3222 >> 6) << 8 | (3223 >> 6)))

    def test_rejects_wrong_approved_fixture(self):
        result = self.run_cli(sha="0" * 64)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("archive does not match", result.stderr)

    def test_rejects_wrong_cache_address(self):
        self.write("item.csv", ["id", "index", "group", "file", "name", "fixture_sha256", "raw_sha256"],
                   [[995, 19, 0, 0, "Coins", self.sha, "abc"]])
        result = self.run_cli()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("invalid cache address", result.stderr)

    def test_reports_invalid_spawn_bounds_and_orphan(self):
        self.write("spawn.tsv", ["spawn_id", "npc_id", "coord_x", "coord_y", "coord_z",
                                "min_coord_x", "min_coord_y", "min_coord_z",
                                "max_coord_x", "max_coord_y", "max_coord_z"],
                   [[1, 999, 3222, 3223, 0, 3224, 3220, 0, 3225, 3225, 0]], delimiter="\t")
        result = self.run_cli()
        self.assertEqual(result.returncode, 0, result.stderr)
        summary = json.loads((self.path / "results" / "summary.json").read_text())
        self.assertEqual(summary["issue_counts"]["missing_npc_definition"], 1)
        self.assertEqual(summary["issue_counts"]["invalid_x_bounds"], 1)


if __name__ == "__main__":
    unittest.main()
