# Design

## Context

See `proposal.md` for the motivation and scope. The existing Angular-managed forms are owned by five standalone components/pages. Angular 22.2.3 already includes Signal Forms in `@angular/forms`; no new dependency is needed. Angular Material controls and `ng-hcaptcha` expose ControlValueAccessor bindings, which `FormField` supports for compatibility.

## Goals / Non-Goals

**Goals:**

- Use a writable signal model as the source of truth for each existing form and a Signal Forms `FieldTree` for validation and control binding.
- Preserve current validators, service calls, submit gating, captcha events, and file upload handling.
- Remove production Reactive Forms and unused `FormsModule` imports.

**Non-Goals:**

- Add new validation or change when existing validation feedback appears.
- Convert the separate native file picker event into a custom Signal Forms control.
- Replace Angular Material controls or change API payloads.

## Decisions

- **One signal model and field tree per existing form group.** The component that currently owns the `FormGroup` will own its corresponding model signal and `form()` result. This keeps state local and avoids a shared wrapper or service. Keeping `FormBuilder` or writing ad hoc input events would leave two form mechanisms; both are rejected.
- **Declare existing rules with Signal Forms schema validators.** Use `required`, `email`, `min`, `max`, `minLength`, and `maxLength` at the same fields where the current validators are configured. Keep submit handlers and service calls in their existing components.
- **Bind existing controls through `FormField`.** Use the standard directive for native inputs, Material CVA controls, and hCaptcha. Do not add custom adapters: Angular's compatibility binding is the narrow mechanism intended for existing CVA controls.
- **Keep native form submission behavior without Angular's `ngSubmit` directive.** Add `novalidate` and use the native `submit` event with `preventDefault()`. This preserves browser-validation and no-navigation behavior after removing the Angular forms modules. Do not use `FormRoot` submission gating for cache actions, whose existing handlers submit even when a configured validator reports invalid.
- **Keep the sprite file picker outside the form field tree.** The current `File` state is already maintained outside its `FormGroup`; retain the same selection event and upload source instead of inventing a file-control abstraction.
- **Skip a delta spec.** This change replaces the form implementation while preserving its externally visible behavior, so it does not add or modify a product requirement.

## Risks / Trade-offs

- [Material or hCaptcha compatibility differs at runtime] → The installed Signal Forms `FormField` supports CVA compatibility; preserve the existing bindings and verify both app builds and lint after migration.
- [Validation or form submission changes during conversion] → Translate each existing validator and handler explicitly, and keep the current login validation messages and gates.

## Migration Plan

Convert the public auth forms, then the admin login and cache manager forms. Remove stale Reactive Forms imports and bindings in the same edits. Run the web build, admin build, and configured frontend lint command. Reverting the source/template edits restores the prior forms without a data migration.
