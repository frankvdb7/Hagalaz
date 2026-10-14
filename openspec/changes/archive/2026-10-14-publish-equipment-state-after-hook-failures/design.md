# Design

## Context

See proposal.md - Why. Both standalone mutation completion and transaction completion already call Equipment effects before Equipment publication. The failure is that Equipment throws its collected hook failures from the effects phase, so the existing caller stops before invoking the publication phase.

## Goals / Non-Goals

**Goals:** Preserve attempt-all hook execution and ensure Equipment publication is attempted before hook failures escape.

**Non-Goals:** Change transaction or mutation-boundary completion ordering, generic publisher failure semantics, or any public API.

## Decisions

- Equipment retains hook exceptions from `CompleteCommittedEffects()` until `PublishCommittedChanges()` has attempted state publication. This uses the existing completion phases and keeps lifecycle-failure policy with Equipment.
- Consume the retained exception list before invoking publication code, so it cannot be replayed if publication throws. Existing completion discard also clears any retained failures.
- If publication succeeds, propagate hook failures with the existing Equipment exception policy. If publication also throws, preserve both failures locally; when there are no hook failures, let the publication exception propagate unchanged. Do not add exception handling to the generic transaction or mutation boundary.
- Rejected alternative: change generic completion loops to catch hook failures and continue. That would make the transaction own Equipment-specific attempt-all semantics and alter publication behavior for every owner.

## Risks / Trade-offs

- [A publication failure can accompany hook failures] → Equipment reports both failures; a publisher failure without hook failures keeps its existing exception behavior.

## Migration Plan

No migration is needed. Update the canonical behavior through the OpenSpec change, implement the Equipment-local completion adjustment, and verify standalone and transaction completion tests.
