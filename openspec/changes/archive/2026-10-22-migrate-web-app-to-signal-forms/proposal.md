# Proposal

## Why

`Hagalaz.Web.App` still manages its user and admin forms with Angular Reactive Forms while the application already uses Angular signals broadly. Convert the app's existing Angular form state to Signal Forms so form models and validation state use the current Angular signal model consistently.

## What Changes

- Convert all 14 `FormGroup` instances across the main login and registration forms, admin login, sprite manager, and type manager to Signal Forms.
- Keep existing required, email, min, max, min-length, and max-length validation semantics, along with submission behavior, Material controls, and hCaptcha integration.
- Keep native file selection and upload behavior intact; its `File` value is currently managed separately from Angular Reactive Forms.
- Remove production Reactive Forms and template-driven Forms imports and bindings from `Hagalaz.Web.App`.
- Leave public UI, routes, service requests, and backend contracts unchanged.

### Non-goals and stop conditions

- Do not redesign form UX, change labels or validation messages, or add cross-field/password matching rules.
- Do not change APIs, routes, cache operations, or how selected files are uploaded.
- Do not introduce a reusable form framework or custom file-control abstraction.
- Stop and report if an existing Material or hCaptcha control cannot preserve its current interaction through Signal Forms' supported compatibility binding.

### Acceptance Criteria

- Every production `FormGroup` in `Hagalaz.Web.App` is represented by a typed Signal Forms model and `form()` field tree.
- The production source contains no `FormBuilder`, `ReactiveFormsModule`, `formControlName`, or `[formGroup]` usage.
- Existing validation, form submission, captcha, and sprite file-upload interactions are preserved.
- The web and admin builds and the configured frontend lint command pass.
- No unrelated package, route, API, or UI changes are included.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a framework implementation migration with no intended behavior change; the existing form behaviors remain the contract.

## Impact

- Angular component TypeScript and templates in `Hagalaz.Web.App/src` and `Hagalaz.Web.App/src-admin`.
- Existing component test setup only where it imports Reactive Forms solely for the migrated component.
- No dependency or lockfile changes; Signal Forms is included in the installed `@angular/forms` package.
