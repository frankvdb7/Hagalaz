# Tasks

## 1. Benchmark correctness

- [x] 1.0 Repair the map appearance fixture and verify both benchmark cases produce measurements while setup checks the responses.
- [x] 1.1 Reproduce the tracked project modification and record the actual diff and cause without discarding changes.
- [ ] 1.2 Move comparison to a clean read-only artifact-consuming job; validate success, missing baseline/report and action failure, plus fork permissions.
- [ ] 1.3 Verify the existing main publisher writes a valid comparable baseline and retains historical reports in a hosted run.

## 2. Benchmark latency

- [x] 2.1 Measure per-class runtimes and map benchmark dependencies to source paths; record required checks and the resulting coverage rationale.
- [x] 2.2 Implement and test a conservative PR selection with a full fallback only if mapping and savings are demonstrated; otherwise document rejection and retain the full run.
- [ ] 2.3 Validate cache-focused and unrelated PR behavior, full main/manual coverage, baseline compatibility and artifact upload using hosted results.

## 3. Superseded work and CodeQL

- [ ] 3.1 Add workflow-specific PR cancellation and unique non-PR groups; validate rapid pushes, independent workflows, main publication and scheduled scans.
- [x] 3.2 Profile at least three CodeQL C# runs by phase and evaluate supported options; preserve all coverage and document measured results or rejected tuning.

## 4. Integration evidence

- [x] 4.1 Validate changed workflow YAML and focused regression checks; confirm existing tests, artifacts, quality gates and security configuration remain intact.
- [ ] 4.2 Record at least three comparable before/after PR timing samples, including queue time, benchmark critical path, CodeQL C#, .NET, web and total runner minutes; report actual savings and limitations.
