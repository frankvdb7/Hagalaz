# Why

Cache Studio and the new model workspace repeat host sizing, section typography, icon sizing, surfaces and focus styling. The compact model inspector also declares Material appearance outside the shared theme.

## What Changes

One goal: give repeated Cache Studio styling one owner using the existing admin theme and Tailwind utilities. Consolidate common section titles, icons, surfaces and selectable-control appearance; remove redundant global defaults and unused rules. Keep specialized viewport, thumbnail and inspector layout local.

## Non-goals

No public-app or launcher changes, application logic, new UI components, dependencies, CSS framework, global redesign or changes to model request ownership. Do not unify styles with genuinely different roles merely because they look similar.

## Acceptance criteria

- AC1: Repeated visual rules have a shared theme owner; repeated host/layout declarations use existing Tailwind utilities. No shared mixin that emits duplicate CSS in each consumer.
- AC2: Model, texture, properties and animation panels retain their current layout, typography and compact field appearance. Use the existing admin focus-ring standard consistently; the three removed identity fields stay absent.
- AC3: Nearby Cache Studio host sizing and tab icons remain correct at desktop and phone widths. Specialized local layout remains scoped and no new CSS leak is introduced.
- AC4: Admin build, existing tests, lint, Fallow and strict spec validation pass. Before/after browser evidence and a concise change inventory identify consolidated rules and deliberate differences.

## Stop conditions

Do not migrate unrelated application styling or create a configurable styling framework. A semantic/layout change beyond consistent focus/hover states requires a separate change.

## Capabilities

### New Capabilities
- `cache-studio-style-ownership`: Shared presentation ownership for existing Cache Studio views.

## Impact

Admin portal theme and Cache Studio component templates/host metadata/styles. Existing functional tests verify the same component interfaces.
