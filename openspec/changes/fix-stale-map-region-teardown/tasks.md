## 1. Lifecycle and ownership correction

- [x] 1.1 Reuse non-resurrecting lookup for stale teardown and existing-state
  mutation paths.
- [x] 1.2 Preserve exact game-object ownership for collision and queued
  existing-object updates.

## 2. Regression coverage

- [x] 2.1 Cover ground-item teardown after permanent region removal.
- [x] 2.2 Cover game-object teardown after permanent region removal.
- [x] 2.3 Cover teardown against a suspended region.
- [x] 2.4 Cover delayed stale game-object update against a replacement.
- [x] 2.5 Retain coverage for legitimate region creation/loading.

## 3. Validation

- [x] 3.1 Run focused map-region tests, full GameWorld tests, integration
  tests, build, strict OpenSpec validation, and `git diff --check`.
