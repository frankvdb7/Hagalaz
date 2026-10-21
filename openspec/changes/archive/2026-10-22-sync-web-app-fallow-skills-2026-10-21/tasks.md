# Tasks

## 1. Sync project-local Fallow skills

- [x] 1.1 Run the Fallow CLI skill-only install with the explicit app root and verify `fallow` and `fallow-setup` are installed by `fallow agent status`.
- [x] 1.2 Update `fallow-review` and add `fallow-codex-onboarding` from official upstream commit `b3bb2ef396b67abe17def46aa1d96e8b1992d905`; verify every destination file matches its source checksum.

## 2. Validate the scoped change

- [x] 2.1 Verify exactly four Fallow skill directories are present, run `git diff --check`, and validate the OpenSpec change with `openspec validate sync-web-app-fallow-skills-2026-10-21 --type change --strict`.
