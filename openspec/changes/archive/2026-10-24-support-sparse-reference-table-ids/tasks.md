# Tasks

## 1. Separate archive-ID capacity from group count

- [x] 1.1 Add `IReferenceTable.Count` and define `ReferenceTable.Capacity` from the greatest present archive ID; preserve sparse IDs in copy and name lookup operations.
- [x] 1.2 Update reference-table encoding and decoding to use actual group counts; add sparse codec round-trip coverage.

## 2. Allow sparse archive reads and report the model

- [x] 2.1 Add cache API regression coverage proving `Read` and `ReadArchive` read an existing high sparse ID and reject an absent ID inside the capacity.
- [x] 2.2 Add Count and ID-space Capacity to the reference-table response and verify the response for sparse IDs.

## 3. Specification and validation

- [x] 3.1 Sync the canonical spec and verify focused cache and service-cache tests, strict OpenSpec validation, and `git diff --check`.

## Workflow follow-up

- Archive this change after all tracked tasks and acceptance checks pass.
