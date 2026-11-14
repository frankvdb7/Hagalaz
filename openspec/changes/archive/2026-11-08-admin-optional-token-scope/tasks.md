# Tasks

## 1. Admin sign-in regression coverage

- [x] 1.1 Add an HTTP-backed store test using the live token response shape with no `scope`; verify the test reproduces the generic sign-in failure before the fix.
- [x] 1.2 Cover an explicit reduced response scope and verify it does not grant admin access.

## 2. Scope response handling

- [x] 2.1 Normalize an omitted response scope to the exact requested scope and safely handle an absent scope in the admin authorization check; verify both regression scenarios pass.
- [x] 2.2 Run the focused admin auth tests and admin build, then confirm the OpenSpec change and diff validate cleanly.
