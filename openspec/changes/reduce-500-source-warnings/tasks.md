# Tasks

## 1. Inventory

- [x] 1.1 Record the MSBuild and JetBrains InspectCode baselines on the current working tree. Preserve the analyzer version, commands, and settings.
- [x] 1.2 Group unique warning instances by owning project and inspection. Identify enough in-scope candidates to meet the threshold after deduplication.

## 2. Refactor in batches

- [x] 2.1 Find affected production callers and existing tests with Rider. Add and run characterization or regression tests before editing production code in each batch.
- [x] 2.2 Correct nullable contracts, asynchronous ownership, obsolete API use, and other code identified by the warning inventory. Use Rider semantic refactorings for applicable changes.
- [x] 2.3 Rerun focused regression tests and measure the warning reduction after each batch. Continue with covered batches until at least 500 distinct warnings are resolved.

## 3. Verify

- [x] 3.1 Run the test projects affected by the refactors and inspect the final diff.
- [x] 3.2 Repeat the clean rebuild and warning-level InspectCode run. Verify a net reduction of at least 500, report the counts by tool with overlap removed, and check for introduced warnings.
