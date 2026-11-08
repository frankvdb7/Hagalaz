## Boundary and reuse

The Angular admin presentation is in scope, including shared theme defaults and the Item Forge reference screen. Reuse the existing CacheItemsStore, draft model, validation, pending-change guard, write dialog, sprite component and model preview. Material owns control appearance and interaction; layout CSS owns grid sizing and spacing.

## Decisions and invariants

- Use outlined MatFormField and MatInput for existing inputs, including readonly ID and unavailable description. Retain IDs, bindings and browser validation constraints.
- Use outlined MatCard for panels and MatChipSet for static metadata. No selectable chip semantics.
- Use MatSlider with its thumb input for zoom and MatExpansionPanel for the model inspector. Keep the existing model component mounted to preserve its lifecycle.
- Remove hand-styled input borders, focus rings, chip shapes and panel colors. Inherit shared admin tokens and public Material overrides.
- Preserve disabled and loading states, error feedback and complete-definition write semantics. Unsupported description text remains readonly.
- Keep responsive layout within this component. Do not change shell structure or add routes.

## Approved scope update

The user requested unifying styles across the entire admin app. Centralize Material appearance in portal-theme.scss, retain page-specific geometry, and reuse the shared request feedback styles. Standard controls use 48px fields and 44px filled/outlined buttons. Toolbar search retains its 36px contextual density through a shared theme selector. Login uses Material labels rather than positioned HTML labels. Sprite rendering, transparency checkerboard and viewport geometry remain owned by their tools.
