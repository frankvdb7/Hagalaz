# Tasks

## 1. Public authentication forms

- [x] 1.1 Convert main-site login and registration models, validators, control bindings, and submit guards to Signal Forms; verify `pnpm run build` compiles both templates.

## 2. Admin authentication and cache forms

- [x] 2.1 Convert admin login validators, hCaptcha and Material bindings, validity display, and submit handling to Signal Forms; verify `pnpm run admin:build`.
- [x] 2.2 Convert all four sprite and seven type manager form models, validators, bindings, and service payload reads to Signal Forms while preserving file selection; verify `pnpm run admin:build`.

## 3. Remove the old forms API and verify integration

- [x] 3.1 Remove obsolete Reactive Forms and FormsModule imports, including the login component's stale test-module import; verify production source has no `FormBuilder`, `ReactiveFormsModule`, `formControlName`, or `[formGroup]` references.
- [x] 3.2 Run the web build, admin build, and configured frontend lint command; review `git diff --check` and confirm the migrated forms retain their existing validation and submit behavior.
