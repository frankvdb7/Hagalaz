## Approved replacement

The user explicitly rejected the upper Definitions/Models/Sprites hierarchy. Move the existing outlined, icon-bearing type row to the cache shell, and add Models as its final entry. The shell is the only navigation owner. Remove the former top row, child tab panel and browser-owned type-switch handler.

## Reuse and invariants

Use Material tab navbar/card and RouterLink, with active kind derived from completed router state. Existing type IDs, routes, stores, filters and backend behavior remain. Type switches clear search/offset and retain page size. Detail routes select their path kind. Models uses `/cache/models`; sprites use `/cache/types?kind=sprites` and Sprite Studio details. No legacy sprite route is restored.

Use shared Material outlined card/field/button styling for the existing model ID form and result. Preserve the model selection and same-ID reload logic. Existing cache guards continue protecting drafts, including tab navigation. Native Material tab pagination handles the long single row on narrow screens.
