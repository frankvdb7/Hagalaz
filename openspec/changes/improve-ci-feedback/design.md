# Design

## Context

The benchmark job uploads JSON then compares in the same checkout. Recorded runs fail when switching branches because a tracked test project changed. The main publish job already downloads the artifact in a fresh checkout and writes to `gh-pages`. The branch and `dev/bench/data.js` exist. The main ruleset currently exposes no required status checks, but requires CodeQL scanning. Recheck this at implementation time.

## Goals / Non-Goals

Use existing Actions and artifacts to provide observable, comparable results and preserve coverage. See the proposal for exclusions.

## Decisions

- GitHub Actions owns job scheduling and cancellation. The benchmark job owns report production; a separate read-only comparison job downloads its artifact into a clean checkout. The existing main publish job alone owns baseline updates. Failed or missing baseline comparison must be visible. No automatic retry owner is added; rerun the workflow through GitHub Actions.
- Diagnose the tracked project diff after the benchmark instead of resetting arbitrary modifications. A clean job avoids coupling comparison to build side effects. Keeping comparison in the dirty job was rejected because it requires discarding or masking changes.
- Replace the obsolete model fixture in the map appearance benchmark with a modern-layout model already exercised by codec tests. Merge all full compressed reports and reject missing statistics before comparing or publishing. Preserve the benchmark's existing setup checks that single and batch responses agree.
- Measure benchmark classes and map source dependencies before selecting a PR subset. The 58-case shared class takes approximately nine minutes in one observed run, so class selection alone is insufficient. Preserve full runs when impact is unknown and keep comparable baseline names and units. Reject a reduced lane if coverage or meaningful savings cannot be demonstrated.
- Scope concurrency groups to workflow and PR identity. Main and scheduled events must not cancel in-progress work; verify GitHub's pending-run semantics as well as cancellation. Avoid altering CodeQL scan configuration without measured evidence.

## Risks / Trade-offs

- Shared runner noise can conceal regressions. Compare repeated runs and retain warmups and iterations.
- Path impact maps can become stale. Conservative fallback and focused mapping tests are required.
- A clean comparison job adds startup time and runner minutes. Measure it along with the benchmark critical path.
- GitHub concurrency may replace pending runs even when cancellation is disabled. Use unique groups for non-PR runs to preserve baseline publication.

## Migration Plan

Validate workflow configuration and focused behavior locally, then verify on hosted PR runs and main publication. Revert workflow changes if comparison or required checks regress; preserve existing benchmark history.
