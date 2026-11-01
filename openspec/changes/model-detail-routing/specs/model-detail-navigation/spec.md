# Spec Delta

## Purpose

Let admins link to and restore a selected cache model and its inspection tab through normal browser navigation.

## ADDED Requirements

### Requirement: Model detail identity belongs to the URL
The admin SHALL expose model details at `/cache/models/:id` and use `/cache/models` as the shared search/table browser. Direct entry, refresh and browser history SHALL restore the selected model. Invalid IDs SHALL show validation without issuing a model request.

#### Scenario: Load and restore a model
- **WHEN** an admin loads model 666 from the lookup form
- **THEN** the URL becomes `/cache/models/666`
- **AND** opening that URL directly or refreshing displays model 666

#### Scenario: Model ID validation
- **WHEN** a detail URL contains zero, a negative value, a non-integer or a value outside the safe integer range
- **THEN** zero is accepted and other invalid values show an invalid-ID state without a model request

#### Scenario: Browser history and failed requests
- **GIVEN** two model selections, including one whose request failed
- **WHEN** the admin uses back/forward or selects another model
- **THEN** the viewer follows the current URL without stale model data
- **AND** retry and same-model reload remain available

### Requirement: Inspection tabs are addressable
The detail URL SHALL preserve the active inspection tab using `tab=textures`, `recolors`, `retextures`, `animations` or `metadata`. Missing or unsupported values SHALL select Model. Selecting Model SHALL omit `tab`. Tab-only navigation SHALL preserve the current model resource and local preview transforms.

#### Scenario: Share and restore a tab
- **WHEN** an admin selects Textures on model 666
- **THEN** the URL is `/cache/models/666?tab=textures`
- **AND** refresh and browser history restore both model and tab

#### Scenario: Default tab and tab-only navigation
- **WHEN** the tab parameter is missing or unsupported, or the admin selects Model
- **THEN** the Model tab is selected
- **AND** changing only the inspection tab does not request the model again or reset preview transforms

### Requirement: Model details follow cache navigation conventions
Model details SHALL retain the selected Models cache tab, identify the model in breadcrumbs, and offer navigation back to model lookup. Compact previews outside the model detail page SHALL continue to work independently of model routes.

#### Scenario: Detail navigation context
- **WHEN** `/cache/models/666` is open
- **THEN** Models is the selected cache resource tab and the breadcrumb identifies model 666
- **AND** the admin can return to model lookup

#### Scenario: Embedded preview compatibility
- **WHEN** an item or definition detail embeds a compact model preview
- **THEN** the preview continues to use its owning definition selection without navigating to a standalone model detail

### Requirement: Models use the shared cache browser
The Models tab SHALL use the same search field, results table, row inspection action, pagination and request feedback as the other cache types. Exact model ID search SHALL open details. Range pages SHALL list only IDs present in cache metadata without decoding geometry. Detail links and return navigation SHALL retain the list page context.

#### Scenario: Browse and inspect sparse IDs
- **GIVEN** a cache with model IDs 0, 2 and 666
- **WHEN** the admin browses the first ID range
- **THEN** the table lists IDs 0 and 2 and provides an Inspect model action to their detail URLs
- **AND** a later page can expose model 666 without loading model geometry for the list

#### Scenario: Return to the same list page
- **WHEN** the admin inspects a model from a non-default list page and returns through Models in the breadcrumb
- **THEN** the list restores its offset and page size
