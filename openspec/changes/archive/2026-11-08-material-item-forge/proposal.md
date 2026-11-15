## Why

Item Forge duplicates Material field and panel styling instead of using the admin application's existing components.

## What changes

Use Material outlined fields, cards, chips, slider and expansion panel in the existing Item Forge route. Retain the supplied reference's identity header, tabs and two-column inspector layout.

## Acceptance criteria

- A0: Admin controls share one Material theme for density, shapes, colors, typography, tables, feedback and expansion panels; page-level copies are removed.
- A1: Form fields, panels, metadata chips, zoom and model disclosure use Angular Material with the existing admin theme.
- A2: Editing, validation, save confirmation, duplication, clipboard, tabs and preview zoom retain their existing behavior.
- A3: Desktop and phone layouts remain usable; admin build, tests, lint and browser checks are recorded.

## Non-goals

No backend, authentication, route, data contract, new dependency or invented description changes. No shared shell redesign. Shared visual tokens and component defaults are explicitly in scope following the user's request to unify the entire admin app.

## Capabilities

### New capabilities
- `material-item-forge`: Material presentation of the existing item editor.

### Modified capabilities
None.

## Impact

Admin shared theme and styles, page-level Material overrides, login field presentation and Item Forge component/templates. Existing stores and runtime behavior remain unchanged.
