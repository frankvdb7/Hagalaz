## 1. Configure the C# scan

- [x] 1.1 Add root jscpd configuration with conservative C# thresholds and narrow generated/build exclusions; verify a real scan includes authored source and tests while excluding generated paths.
- [x] 1.2 Review the scan's clone-pair count, duplicated lines and percentage, hotspots, and false positives; verify any threshold or exclusion adjustment against actual findings.

## 2. Enforce the pull-request gate

- [x] 2.1 Add a dedicated PR-only CI job with a full-history checkout, exact jscpd version, target-branch baseline, and empty-scan failure; verify the workflow selects an available base ref and does not construct one on pushes.
- [ ] 2.2 Generate and upload SARIF with job-scoped permissions and an explicit scanner-exit assertion; verify the report identifies both clone locations and workflow syntax is valid.

## 3. Validate regression behavior

- [x] 3.1 Verify the unchanged repository passes against the git-ref baseline while historical clones remain reported.
- [x] 3.2 Introduce a temporary qualifying C# clone, verify the baseline gate exits nonzero, remove it, and verify the gate passes again.
- [x] 3.3 Prove an empty scan fails and ignored generated/build paths do not contribute findings; remove all temporary validation files.
- [ ] 3.4 Validate the OpenSpec change, review the cumulative diff/status for scope and temporary artifacts, and record any CI upload limitation.
