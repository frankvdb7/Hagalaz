## Context

The public app supplies the brand palette, lightning asset and Semplicita Pro. The admin currently wraps only cache routes. Cache services support reads/searches and narrowly scoped writes; character statistics do not provide administrative account operations. No verified portal contracts provide world operations, economy, service telemetry or audit history.

## Decisions

- A lazy guarded parent shell owns responsive drawer visibility and sign-out. Route children keep their existing page-scoped stores and form ownership. Guard both entry and child navigation.
- Reuse generated amber palettes and font mixins. Scope navy system roles and public Material overrides to the portal so login/denied retain their current styling. Semantic accents live in one SCSS module.
- Use one small integration page for the six unavailable sections, with route-specific copy and required backend capabilities. It performs no HTTP calls and exposes no simulated actions.
- Overview uses static navigation and honest empty states for absent telemetry. Its archive-count card reuses a locally provided CacheTypesStore to load real archive sizes on demand, with failure/retry feedback. No store is added for absent metrics.
- Preserve cache forms and add a real search-result table with selection into ID inspection. Keep arbitrary definitions read-only; all writes continue through existing validated forms.
- No new API ownership, retries or caches. Existing stores own pending/error/retry behavior and components own preview URL cleanup.

## Alternatives

A screenshot replica with sample rows would misrepresent live state. A second application or shared generic editor adds unnecessary ownership. Existing Material components plus targeted theme overrides satisfy the reference without either.

## Verification

Keep existing store and sprite tests. Add real-router guard/navigation tests, shell controls, unavailable states and Types form/table tests. Exercise rendered desktop/mobile screens against test-only HTTP fixtures, separate from real backend persistence.

## Cache Studio layout revision

The user approved replacing the legacy form arrangement to match the revised Cache Studio reference. Category tabs and one query/paging bar lead into a selectable list and a contextual inspector/editor. Name searches use only supported search APIs; blank queries browse supported ID ranges; numeric queries inspect an ID. The selected definition populates the existing mutation forms. Read-only fields and raw metadata remain distinct from supported writes. No publication states, revision number, export or create actions are invented. This revises the frontend composition without changing API contracts or request ownership.

The user subsequently removed the Definitions/Sprite tools header buttons and the Archive tools panel. Blank-query range browsing remains in the main search bar; the redundant standalone range form was removed.

Category tabs now share the search card above its fields, using Material tab navigation with a linked results panel. A regression test checks containment, ordering and category activation. The 11 focused Types page tests and scoped Biome checks pass. Fixture-backed browser checks confirm desktop and 390px layouts without page overflow. The existing test polyfills warning remains unchanged.

## Toolbar reference revision
The toolbar follows the supplied compact breadcrumb/search/bell/settings/avatar reference using Material controls and existing theme tokens. Search filters the existing portalNavigation destinations; it does not search backend realm data. Settings uses the existing route, the account menu retains the AuthStore logout flow, and the notification menu states that notifications are not connected. Phone layouts wrap search below the controls. Acceptance: keyboard-accessible destination search, truthful notification feedback, account sign-out, and no page overflow. No new backend contracts or state owner are introduced.

## Definition pagination revision
Replace numeric offset/results inputs with a Material paginator in the definition list. Reuse the page-scoped store, archive-sizes endpoint and search totalMatches; range pages represent definition ID slots, including sparse IDs. Page changes use the submitted query, new searches/category changes reset the page, loading blocks duplicate requests, and numeric lookups do not paginate. No API changes or invented totals. Verify real range/search requests, final-page bounds, page-size changes and category resets.

The user removed the ID-range pagination note and the live-workspace banner. Keep the paginator and existing mutation confirmations; the general explanatory notices are no longer rendered. Updated route expectations and both focused suites pass (30 tests); scoped Biome and whitespace checks pass. Browser confirms both texts absent.

## Startup surface consistency
Apply the existing admin color token overrides to html immediately after mat.theme. Body and the document canvas use the same Material surface token before bootstrap and after the portal loads. Keep component overrides scoped to the portal and overlays. This fixes the warm-to-navy startup change through the documented Material theming API, with no inline fallback colors or runtime theme toggling.
