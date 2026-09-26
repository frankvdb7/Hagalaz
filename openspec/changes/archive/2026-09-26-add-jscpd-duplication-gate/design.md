## Context

See the proposal and `csharp-duplication-gate` delta spec. The existing CI workflow has separate build, OpenSpec, and web app jobs; only the web app checkout currently fetches full history. C# source and tests live across root-level projects. The current CodeQL workflow has a separate security responsibility.

## Goals / Non-Goals

**Goals:** Keep one jscpd configuration and one dedicated CI duplication job. Let jscpd own clone detection, baseline comparison, and report generation.

**Non-Goals:** No source refactoring, Angular dependency, clone comparison script, persistent baseline file, or change to CodeQL.

## Decisions

### Use the official jscpd action and exact v5.3.2 engine

Use the official action at its `v5.3.2` release tag with its `version: 5.3.2` input. The action performs installation and invokes the native jscpd engine. An exact release is reproducible while remaining readable alongside the existing version-tagged actions. The released action captures the scanner exit code as an output so report steps can run; CI must explicitly assert that output after report upload.

Alternatives: A repository Node package would couple the C# gate to frontend tooling. A hand-written binary installer would duplicate the action's supported installation path. An unversioned `v5` tag or default `latest` engine would drift.

### Use a target-branch git-ref baseline only on pull requests

The duplication job runs only on `pull_request`, checks out full history, and passes `origin/${{ github.base_ref }}` to jscpd's native `baseline-from-ref` comparison. The job does not run on ordinary pushes, for which `github.base_ref` is absent; existing CI jobs still run there. This gives the target branch one authoritative baseline and avoids a separately maintained fingerprint file.

Alternative: A committed baseline requires intentional refresh and can drift from the actual target branch. A global percentage threshold would block historical debt or miss small regressions.

### Use narrow C# exclusions and conservative clone thresholds

Start with `csharp`, `minLines: 10`, `minTokens: 80`, and mild mode. Exclude `bin`, `obj`, EF migrations, and generated `.g.cs` / `.Designer.cs` files. Keep tests and authored script files. Finalize these settings after a real scan; only observed false positives justify tuning.

### Publish SARIF with explicit failure visibility

The action will generate console, JSON (needed for its output parsing), and SARIF reports. Disable its built-in SARIF upload because that step uses `continue-on-error`. Upload the generated SARIF with an explicit Code Scanning step and job-level `security-events: write`; assert the scanner's exit-code output in a final step. A narrow ignore for the generated `report/` directory prevents local report files from entering the diff.

## Risks / Trade-offs

- [Git ref missing in CI] → Use full-history checkout; a missing ref produces a failing scan and is validated locally.
- [Action captures scanner failure for reporting] → Assert its output after SARIF upload.
- [SARIF upload permission or GitHub availability] → Give only this job `security-events: write` and leave upload failure visible. Local validation can verify report structure, but cannot prove GitHub's hosted upload without a CI run.
- [Existing duplication volume] → Retain baseline comparison and avoid a global threshold or broad source exclusions.

## Migration Plan

Add the OpenSpec record, root config, report ignore, and dedicated CI job. Validate existing and deliberately introduced clones, empty scans, exclusions, and SARIF output before completion. Rollback is a revert of those tooling files; no runtime data changes.

## Local Validation Evidence

- jscpd 5.3.2 analyzed 2,246 C# files: 559 clone pairs, 11,304 duplicated lines (4.42%). The largest pairs occur in priority queue implementations, familiar NPC scripts, and repeated test setup. Tests and authored scripts remain included; no thresholds were relaxed.
- Comparing the unchanged tree with `origin/main` exited 0 with 559 historical pairs and zero new pairs. Two temporary C# copies raised the count to 560 with one new pair and exit 1; removal restored exit 0.
- A zero-file pattern exited 1 via `--fail-on-empty`. Copies in `.g.cs`, `.Designer.cs`, `obj`, and `Migrations` paths changed neither the scanned-file count nor clone count.
- The local SARIF report is version 2.1.0 and gives all 559 results a primary location, related location, and fingerprint. `actionlint` validates the workflow. PR #508's hosted duplication and OpenSpec jobs passed; GitHub Code Scanning analysis `1844553038` accepted 559 jscpd results.
