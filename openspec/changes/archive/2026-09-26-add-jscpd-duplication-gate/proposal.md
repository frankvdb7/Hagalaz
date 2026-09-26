## Why

Hagalaz has a large C# codebase with existing copy/paste duplication. Issue #507 asks for visible findings and a pull-request gate that blocks new clones without making historical duplication fail unrelated work.

## What Changes

- Add one root jscpd v5 configuration for C# production and test source, with narrow generated/build exclusions.
- Add a dedicated CI duplication job that compares pull requests with the base commit recorded in their event, fails on new clones or an empty scan, and publishes SARIF findings.
- Run jscpd against the current repository and validate the baseline, new-clone, exclusion, and empty-scan behaviors.

### Non-goals

- Do not refactor existing C# clones or change application behavior.
- Do not add a global duplication-percentage gate, dashboard, MCP integration, pre-commit framework, or general quality platform.
- Do not replace CodeQL, Roslyn analyzers, or tests, or add jscpd to the Angular application.

### Acceptance Criteria

- The committed configuration scans real C# source, including tests, while omitting generated/build/vendor-owned C# by narrow rules.
- Existing clones appear in the report and do not fail an unchanged or unrelated pull request.
- A new clone above the chosen thresholds fails the pull-request job; removing it restores success.
- A scan with no analyzed source fails clearly.
- The exact base commit recorded in the pull-request event is available for comparison; push handling never uses an empty PR base.
- SARIF identifies both clone locations and is uploaded to Code Scanning when CI permissions allow it.
- The baseline statistics, thresholds, and any remaining verification limits are recorded.

### Stop Conditions

- If native jscpd git-ref comparison cannot reliably gate this repository, revise the design before introducing a committed baseline.
- If SARIF upload cannot work with narrowly scoped CI permissions, record the limitation and use a useful CI report without changing CodeQL.

## Capabilities

### New Capabilities

- `csharp-duplication-gate`: Repository C# clone reporting and pull-request regression behavior.

### Modified Capabilities

None.

## Impact

The change is limited to a root jscpd configuration, the CI workflow, an OpenSpec record, and a narrow report-output ignore if needed. It reuses jscpd's native clone comparison and SARIF reporter, GitHub Actions checkout, and Code Scanning upload. No production service, test project, package manifest, or public API changes.
