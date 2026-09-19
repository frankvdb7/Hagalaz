## 1. Lifecycle and ownership correction

- [x] 1.1 Reuse non-resurrecting lookup for stale teardown and existing-state
  mutation paths.
- [x] 1.2 Preserve exact game-object ownership for collision and queued
  existing-object updates.
- [x] 1.3 Treat a removed dimension as absent for stale existing-state work.

## 2. Regression coverage

- [x] 2.1 Cover ground-item teardown after permanent region removal.
- [x] 2.2 Cover game-object teardown after permanent region removal.
- [x] 2.3 Cover teardown against a suspended region.
- [x] 2.4 Cover delayed stale game-object update against a replacement.
- [x] 2.5 Retain coverage for legitimate region creation/loading.
- [x] 2.6 Cover stale object and ground-item work after dynamic dimension
  removal and object work after numeric dimension ID reuse.

## 3. Validation

- [x] 3.1 Run focused map-region tests, full GameWorld tests, integration
  tests, build, strict OpenSpec validation, and `git diff --check`.
