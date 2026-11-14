## Context

The API is read-only for items. CacheItemsStore already owns detail, range and search resources. The original shared design described three columns. The approved revision replaces the left navigator with a breadcrumb autocomplete and gives the detail workspace two columns.

## Goals / Non-goals

Provide accurate inspection and explicit navigation using existing ownership. Do not implement mutation, model rendering or server item-instance data.

## Decisions

- Keep ItemsDetailPageComponent and its route. One page-scoped CacheItemsStore owns detail and picker search requests. A shell-scoped template slot renders the page-owned picker in the main breadcrumb without copying item state.
- Use Signal Forms and Material autocomplete in a CDK connected overlay. Search names after a short debounce, offer direct navigation for valid nonnegative IDs, and show loading/errors/empty states. Close on selection, Escape or outside click, restoring focus to the trigger. Archive browsing remains on the shared types page.
- Distinguish absent fields from false/zero and negative sentinel references. Preserve action indices and empty slots. Do not infer links from adjacency or claim reciprocal relationships.
- Add NoteTemplateId, LendId, LendTemplateId, BoughtItemId, BoughtTemplateId and InterfaceModelId to the existing response, populated directly from IItemType. Existing mapping is shared by detail/range/search.
- Keep model ID informational. Preview explicitly explains that no item render API exists. Equipment numbers are cache metadata, not gameplay bonuses. Raw data shows the actual response.
- Existing HTTP resources own cancellation and errors. Route changes replace the detail request; retry reloads the selected ID. Picker failures do not replace selected detail state.
- Extend the main toolbar breadcrumb only. Use existing theme tokens in SCSS and responsive grid collapse without hiding inspection fields.

## Alternatives

A new item store or generic schema editor would duplicate ownership and imply unsupported writes. A rendered placeholder image would misrepresent cache capabilities. Both are rejected.

## Future item mutation

See the frontend item-forge document for an explicit typed patch, authorization, revision checks, codec round-trip preservation and mutation regression tests. This change introduces no mutation mechanism.

## Verification

Test route changes, exact mapping, empty slots, absent values, links including zero, autocomplete selection, errors, dismissal and template cleanup. Run admin coverage/build, Biome, Fallow and OpenSpec checks. Inspect desktop/mobile with coherent external fixtures; report that separately from live backend validation.
