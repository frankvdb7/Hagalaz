# Tasks

## 1. Baseline and tests

- [x] 1.1 Refresh the build and InspectCode baselines and confirm at least 100 distinct in-scope diagnostics.
- [x] 1.2 Add and run focused characterization tests before editing production source. Capture the deterministic Game Scripts assembly baseline.

## 2. Refactor

- [x] 2.1 Remove confirmed redundant imports and correct invalid XML documentation.
- [x] 2.2 Review the diff for unrelated changes and confirm existing scripts and signatures are preserved.

## 3. Verify

- [x] 3.1 Run the focused and full Game Scripts tests without coverage. Compare the compiled assemblies.
- [x] 3.2 Repeat the solution rebuild and InspectCode run. Verify a net reduction of at least 100 additional warnings and report introduced diagnostics.
