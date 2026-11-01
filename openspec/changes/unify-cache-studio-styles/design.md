# Design

## Boundary and reuse

The existing admin portal theme owns Material tokens, public overrides and shared focus rules. Tailwind already supplies layout utilities. Reuse both instead of adding Sass mixins or a parallel token system.

## Decisions

- Put reusable Studio surface, section-title, icon and choice styles in portal-theme.scss. Namespaced classes are applied explicitly to consumers under the admin portal, avoiding broad element overrides.
- Move the existing compact inspector Material overrides into the same theme's contextual-variant section. Keep property-grid/skin-group behavior local.
- Replace repeated block/min-width host rules with host Tailwind classes. Preserve container-query ownership on the existing hosts.
- Remove model-local focus rules in favor of the existing admin ring. Include summaries and scrollable raw content in that shared rule. Keep intentional texture-border versus group-row selection treatments distinct.
- Group repeated viewport toolbar/footer positioning within their existing stylesheet. Remove unused notice styling and form-field sizes already supplied globally.

## Ownership and invariants

One global theme rule owns each shared visual primitive; templates opt in through explicit classes. Component styles own only their specialized layout. No request, form, selection, rendering or cleanup state changes. Existing breakpoints and recent identity-field removal remain intact.

## Rejected alternatives

Per-component Sass mixins would deduplicate source while still emitting duplicate CSS. Replacing panels with new wrapper components would add unnecessary application structure. Broad global h2/icon/button overrides would leak appearance into unrelated views.

## Verification

Capture model workspace computed styles and desktop/phone screenshots before and after. Inspect default, selected, focus, texture and animation states. Run the existing admin tests, production build, lint and Fallow; record meaningful reductions and remaining role-specific styles. No new tests that mirror CSS declarations.
