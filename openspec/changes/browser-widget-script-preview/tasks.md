# Tasks

## 1. Cache service

- [x] 1.1 Emit the versioned RuneScript program and its readable rendering from decoded CS2. Run fixture tests for exact operands, branch and switch targets, and unsupported instruction indices.
- [x] 1.2 Add the guarded widget program load. Run endpoint tests for callback roots, nested calls, recursion, missing callees, and reuse of decoded script entries within the response.

## 2. Browser execution

- [x] 2.1 Implement the TypeScript interpreter and widget effects. Run tests for nested calls with returned stack values, pointer arguments, trigger filters, rollback, and execution limits.
- [x] 2.2 Wire Play and Stop to the local runtime and remove the server execution path. Run Angular regressions for open, pointer, and manual events without execution requests, plus reset and navigation during loading.

## 3. Integration

- [ ] 3.1 Build Cache service and Admin and verify in a signed-in preview that a cached callback changes the widget locally after the script bundle loads. Reconcile the superseded server execution requirement before archive.
