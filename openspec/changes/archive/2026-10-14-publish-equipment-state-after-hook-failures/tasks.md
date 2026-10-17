# Tasks

## 1. Equipment completion behavior

- [x] 1.1 Retain attempt-all Equipment hook failures until Equipment publication and propagate failures afterward; verify standalone and transaction regressions assert committed state is published.
- [x] 1.2 Preserve simultaneous hook and Equipment publication failures; verify both original failures survive in the focused regression.

## 2. Validation and specification

- [x] 2.1 Update canonical Equipment completion requirements and validate the delta with `openspec validate publish-equipment-state-after-hook-failures --strict`.
- [x] 2.2 Run focused and full GameWorld tests plus `git diff --check` to verify the lifecycle ordering and final patch.
