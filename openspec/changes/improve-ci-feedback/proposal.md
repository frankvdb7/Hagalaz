# Proposal

## Why

PR feedback is delayed by the full benchmark run, and its comparison currently fails in a dirty checkout while appearing successful. Superseded CI runs also consume runner time.

## What Changes

- Repair benchmark comparison using the existing artifact and benchmark action in a clean checkout, with visible failure or unavailability and no PR baseline writes.
- Investigate a measured, coverage-preserving PR benchmark lane with a full-run fallback; retain complete main and manual reports.
- Cancel superseded PR runs independently per workflow while preserving main and scheduled runs.
- Profile CodeQL before considering changes and record hosted timing and validation evidence.

## Capabilities

### New Capabilities

- `ci-performance-feedback`: Trustworthy benchmark feedback and cancellation of superseded CI work.

### Modified Capabilities

None.

## Impact

GitHub Actions workflows, benchmark artifacts and reporting. Reuse checkout, artifact actions, BenchmarkDotNet and the existing benchmark action; no new CI platform or dependency.

## Acceptance criteria

- A clean comparison uses a real main baseline and reports failures visibly; fork PRs cannot publish.
- Main and manual runs retain full benchmark coverage; any reduced PR lane maps impact conservatively and falls back to full when uncertain.
- PR cancellation remains workflow-specific and does not cancel main publication or scheduled scans.
- Tests, security and quality checks remain intact. Validate workflow syntax and record focused hosted runs, including three comparable before/after timing samples with queue and runner time distinguished.
- Document any optimization rejected for coverage, noise, cost, or required checks, and report observed rather than assumed savings.

## Non-goals and stop conditions

No test or security reduction, speculative caching, dependency churn, source behavior changes, or GameClient workflow changes. Stop before a reduced lane if coverage cannot be mapped confidently, or before reporting hosted validation without actual runs.
