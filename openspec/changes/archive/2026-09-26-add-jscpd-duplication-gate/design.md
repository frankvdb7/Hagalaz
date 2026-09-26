## Context

See the proposal and `csharp-duplication-gate` delta spec. The existing CI workflow has separate build, OpenSpec, and web app jobs; only the web app checkout currently fetches full history. C# source and tests live across root-level projects. The current CodeQL workflow has a separate security responsibility.

## Goals / Non-Goals

**Goals:** Keep one jscpd configuration and one dedicated CI duplication job. Let jscpd own clone detection, baseline comparison, and report generation.

**Non-Goals:** No source refactoring, Angular dependency, clone comparison script, persistent baseline file, or change to CodeQL.

## Decisions

### Use the official jscpd action and exact v5.3.2 engine

Pin the official action to commit `beb552864d3342241fbcca0f2c46c6248b0426df`, the commit backing v5.3.2, and retain `version: 5.3.2` for the engine. The commit SHA fixes the action source; the version input selects the jscpd engine. The action captures the scanner exit code as an output so report steps can run; CI explicitly asserts that output after the optional upload step.

Alternatives: A repository Node package would couple the C# gate to frontend tooling. A hand-written binary installer would duplicate the action's supported installation path. A release tag, unversioned `v5` tag, or default `latest` engine can move and is insufficient for pinning third-party action code.

### Use the PR event's base commit as the git-ref baseline

The duplication job runs only on `pull_request`, checks out full history, and passes `${{ github.event.pull_request.base.sha }}` to jscpd's native `baseline-from-ref` comparison. That commit is the exact base captured by the PR event used to create the synthetic merge commit. The job does not run on ordinary pushes; existing CI jobs still run there. This avoids comparing with a later branch tip and avoids a separately maintained fingerprint file.

Alternatives: `origin/${{ github.base_ref }}` can advance after the PR event and yield a different baseline. A committed baseline requires intentional refresh and can drift from the PR base. A global percentage threshold would block historical debt or miss small regressions.

### Use narrow C# exclusions and conservative clone thresholds

Start with `csharp`, `minLines: 10`, `minTokens: 80`, and mild mode. Exclude `bin`, `obj`, EF migrations, and generated `.g.cs` / `.Designer.cs` files. Keep tests and authored script files. Finalize these settings after a real scan; only observed false positives justify tuning.

### Publish SARIF with explicit failure visibility

The action will generate console, JSON (needed for its output parsing), and SARIF reports. Disable its built-in SARIF upload because that step uses `continue-on-error`. Upload the generated SARIF with an explicit Code Scanning step and job-level `security-events: write` for same-repository, non-Dependabot PRs; fork and Dependabot PRs retain console findings. Assert the scanner's exit-code output in a final step for every PR. A narrow ignore for the generated `report/` directory prevents local report files from entering the diff.

## Risks / Trade-offs

- [Base commit missing in CI] → Use full-history checkout; a missing commit produces a failing scan and is validated locally.
- [Action captures scanner failure for reporting] → Assert its output whether or not SARIF upload runs.
- [SARIF upload permission or GitHub availability] → Skip upload for fork and Dependabot PRs; leave upload failure visible for same-repository PRs. Local validation can verify report structure, but cannot prove GitHub's hosted upload without a CI run.
- [Existing duplication volume] → Retain baseline comparison and avoid a global threshold or broad source exclusions.

## Migration Plan

Add the OpenSpec record, root config, report ignore, and dedicated CI job. Validate existing and deliberately introduced clones, empty scans, exclusions, and SARIF output before completion. Rollback is a revert of those tooling files; no runtime data changes.

## Validation Evidence

- Hosted jscpd 5.3.2 analyzed 2,245 C# files: 559 clone pairs, 11,304 duplicated lines (4.42%). The largest pairs occur in priority queue implementations, familiar NPC scripts, and repeated test setup. Tests and authored scripts remain included; no thresholds were relaxed.
- Comparing the unchanged tree with the PR base commit exited 0 with 559 historical pairs and zero new pairs. Two temporary C# copies raised the count to 560 with one new pair and exit 1; removal restored exit 0.
- A zero-file pattern exited 1 via `--fail-on-empty`. Copies in `.g.cs`, `.Designer.cs`, `obj`, and `Migrations` paths changed neither the scanned-file count nor clone count.
- The local SARIF report is version 2.1.0 and gives all 559 results a primary location, related location, and fingerprint. `actionlint` validates the workflow. PR #508's hosted duplication and OpenSpec jobs passed; GitHub Code Scanning accepted 559 jscpd results.
