## Design

The existing cache types page and page-scoped stores remain the sole owners of navigation and request state. Separate the category navigation and search into Material cards, add relevant Material icons and compact table presentation. Preserve all 13 supported categories, query behavior, pagination and real item/sprite thumbnail components. Display only returned data; the API does not expose the screenshot's item descriptions and the app has no equivalent sort, filter or view mode, so do not fabricate those controls.

Reuse Material theme overrides and existing components instead of another design system. No new retry or data owner is introduced. Update the existing test that asserts the previous tab containment to verify the new layout and existing category behavior.

Each row has a Material menu with an inspect action using the same detail route and preserved query parameters as the existing name link. A shared lazy menu receives the selected row and kind from the trigger. No unsupported mutations or extra routes are added.

## Verification

Build and lint the admin, run its focused tests, validate the change, and compare a fixture-backed desktop view with the reference plus a narrow layout. Keep fixture behavior and live-service validation separate.

The initial fixture comparison used sample definitions and returned errors for item images. A subsequent authenticated browser comparison at 1649×954 confirmed the first ten real item sprites; compacting their boxes kept the paginator in view. Descriptions and filter, sort and view-mode controls in the reference remain outside the supported API and are not simulated. The narrow layout and interactions were checked with fixtures; the live comparison was read-only.
