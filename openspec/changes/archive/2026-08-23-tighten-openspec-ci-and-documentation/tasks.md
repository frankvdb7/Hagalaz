## 1. CI validation

- [x] 1.1 Add a dedicated CI job that installs Node 24 and runs `@fission-ai/openspec@1.10.0` with strict active-artifact validation, verifying the job fails on malformed OpenSpec content.
- [x] 1.2 Add the non-interactive archived-task validation command to the same job and verify the workflow YAML remains valid.

## 2. Repository guidance

- [x] 2.1 Update `openspec/README.md` to document `openspec-update-change`, `openspec-sync-specs`, and the distinction between project configuration and the global workflow profile; verify the documented commands match the generated skills.

## 3. Final verification

- [x] 3.1 Run strict active validation, archived validation, and `git diff --check`; verify all pass and review the final diff for unrelated changes.
