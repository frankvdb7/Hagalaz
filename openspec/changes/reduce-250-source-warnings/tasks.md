# Tasks

## 1. Inventory

- [x] 1.1 Capture the complete warning-level InspectCode report and group distinct tracked-source warnings by inspection and owning project. Select enough confirmed instances to achieve a net reduction of at least 250.

## 2. Test and fix each batch

- [x] 2.1 Identify callers and existing tests. Add and run focused tests for uncovered behavior before changing the corresponding production code. Record the baseline results.
- [x] 2.2 Fix the confirmed warnings in coherent batches. Rerun the tests for each batch and use Rider to check the affected files for new diagnostics.

## 3. Verify the solution

- [x] 3.1 Rebuild in Rider and rerun the same InspectCode command. Verify a net reduction of at least 250 distinct warnings and check for newly introduced source warnings.
- [x] 3.2 Run the affected test projects, compare broader test failures with the baseline, and review the diff for unrelated changes.
