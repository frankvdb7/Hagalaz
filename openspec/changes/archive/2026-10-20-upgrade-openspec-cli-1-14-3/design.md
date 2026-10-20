# Design

## Decision

Use the current stable `@fission-ai/openspec` release, 1.14.3, for both CI validations and generated Codex instructions. Keep the CI version exact so validation remains reproducible. Refresh generated files with the installed CLI rather than editing generated instructions by hand.

Resolve the strict-validator findings with spec-only edits. Shorten the 40 flagged requirement descriptions to 500 characters or fewer, preserving their rules and all existing scenarios. Keep multi-part constraints where the existing scenarios require them; do not split or rename requirements when that would disturb existing capability references. For reconnect-intent evidence, keep the requirement as a concise provenance rule and retain every listed client fact in its existing scenario. The user confirmed that the two empty active change placeholders make no behavior change, so mark those with `skip_specs: true`.

## Ownership and Reuse

- OpenSpec's global custom profile remains the source of truth for which workflows are selected. The repo update consumes that profile and does not change it.
- The existing OpenSpec CI job remains the validation owner; only its pinned package version changes.
- `openspec/README.md` remains the repository's workflow documentation.
- The existing OpenSpec requirement and scenario text remains the behavior source of truth; these edits only make that text conform to the latest strict length rule.
- The two empty placeholders remain available as active changes; metadata makes their no-spec intent explicit without deleting them or inventing proposal content.

The current profile already selects `verify`; refreshing the Codex integration adds its generated skill. The five additional workflows offered by the new CLI are not selected and remain out of scope.

## Alternatives

- Pinning the floating `latest` tag in CI was rejected because it makes validation change without a repository update.
- Editing generated skills directly was rejected because `openspec update` would overwrite those edits.
- Expanding the machine-wide workflow profile was rejected because it affects other repositories and is not needed for this repo's selected workflows.

## Validation

Run the same strict active-artifact and archived-task checks with CLI 1.14.3 that CI will run, then run `git diff --check` and review the complete scoped diff for meaning or scenario loss.
