# Design

## Context

Rider rebuilt `Hagalaz.sln` successfully and reported 706 build problems. Its project Problems view initially exposed only dependency advisories, and the build response retains at most 100 entries. Use a full JetBrains InspectCode report to select source diagnostics.

## Decisions

- Use InspectCode 2026.3.0.2 at warning severity with the `10.0.404` SDK's `MSBuild.dll` passed as `--toolset-path`. Use the same configuration before and after, and deduplicate by tracked file, diagnostic, and occurrence. Count overlapping build and inspection results once.
- Group fixes by inspection and owning project. Confirm each candidate in current source, check callers with Rider when a symbol changes, and use its semantic refactorings for applicable changes. Keep existing callback and public signatures where callers rely on them.
- Use existing tests where they assert the affected behavior. Add focused characterization tests for missing coverage, then run the relevant tests before editing source. Run the same tests again after the fix.

## Risks and validation

- A passing solution build can hide analysis failures in the inspection report. Check project-load and analysis errors before counting the diagnostics.
- The baseline solution test run showed existing database migration failures. Use focused suites to verify each batch and compare broader failures against the baseline. The unchanged Game Scripts test project passed all 1,072 tests.

## Verification results

- Rider rebuilt the solution successfully before and after the cleanup. Its build problem count fell from 706 to 681, including diagnostics also counted by InspectCode.
- Matching warning-level InspectCode runs used the existing build output with `--no-build`. Distinct warnings in tracked C# files fell from 2,647 to 2,386, a net reduction of 261. No warning message increased in count within its file and diagnostic.
- Both InspectCode runs reported resolution errors in the Game Scripts and GameWorld test projects. The count varied between scans, primarily for references to the linked `ComposedTestContainer` helper. Rider successfully compiled and ran both projects. The warning counts describe the InspectCode reports.
- Rider ran five focused dialogue and widget cases successfully before production edits, including the two new Fight Cave cases. The affected-project run then passed 4,808 cases with three ignored.
- The full solution test run reported the same 93 Docker-dependent failures before and after the cleanup.
