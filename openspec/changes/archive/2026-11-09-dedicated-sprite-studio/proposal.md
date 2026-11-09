# Proposal

## Why

Sprite details currently reuse a general definition editor and a utility-oriented sprite manager, so frame inspection and whole-sprite replacement are not presented as one clear workflow. A dedicated Sprite Studio will make the existing sprite API easier and safer to use while retaining the current authenticated route and backend contract.

## What Changes

- Replace the generic sprite detail presentation with a visual-first workspace for frame browsing, image preview, metadata inspection, and whole-sprite replacement.
- Keep `/cache/types/sprites/:id`, the standalone `/cache/sprites` utility page, and the existing metadata, frame-image, and replacement API operations.
- Make frame selection, image loading, and replacement feedback accurate when requests overlap, fail, or change the sprite's frame count.
- Provide keyboard-accessible frame navigation, preview controls, and responsive Material layout based on the attached reference.
- Add focused tests for supported interactions, replacement behavior, request races, and image URL cleanup.

## Scope Boundary

### In scope

- A three-region sprite detail workspace: frame list, primary preview, and Material inspector tabs for properties and replacement.
- Lazy frame image requests, selected-frame metadata, zoom/fit and preview background controls, current-frame PNG download, and empty/loading/error states.
- Local PNG validation and before/after preview followed by explicit confirmation for the existing whole-sprite mutation.
- Refreshing sprite metadata and preview after a successful replacement, while preserving usable state after a failed or canceled replacement.
- Targeted regression tests and validation for the admin application.

### Non-goals

- New backend endpoints, sprite attributes, or mutations for an individual frame.
- A canvas editor, pixel painter, animation timeline, sprite version history, or generic asset-editor framework.
- Reworking the portal shell, other cache definition pages, or standalone sprite search beyond changes required to preserve its current behavior.
- Adding dependencies or changing authentication and authorization behavior.

## Acceptance Criteria

1. The existing sprite detail route renders the dedicated workspace and obtains ID, dimensions, and frame count from the existing API.
2. Frame navigation fetches only requested previews, eventually displays the latest selected frame, supports keyboard use, and handles zero and one frame correctly.
3. Preview controls preserve the source image and aspect ratio, expose zoom/fit and background mode accessibly, and download the selected frame as PNG.
4. Replacement accepts a valid PNG, provides a before/after preview, explains that the entire sprite is replaced, and submits only after explicit confirmation.
5. A canceled or failed replacement is not reported as successful; a successful response refreshes metadata and preview and reconciles the selected frame with the new frame count.
6. Changing sprite IDs cannot leave stale metadata or images visible as belonging to the new sprite, and superseded or destroyed blob URLs are revoked.
7. The standalone sprite utility page and existing API contract continue to work.
8. Focused admin tests cover the behavior above, and the admin build, test, Biome lint, Fallow audit, and OpenSpec validation pass. Report measured coverage for changed code where the tooling provides it.

## Stop Conditions

- Stop and propose a separate change if implementation requires a backend contract change, a new dependency, changes to shared portal navigation, or significant redesign of the standalone sprite utility.
- Stop if the existing API cannot provide a required displayed value or operation; do not invent a field or endpoint to fill the gap.
- Keep the solution within the existing Angular Material, NgRx Signal Store, Signal Forms, and cache service boundaries.

## Capabilities

### New Capabilities

- `cache-sprite-studio`: Inspect decoded sprite frames and safely replace a complete sprite through the existing cache API.

### Modified Capabilities

None.

## Impact

- **Frontend:** `Hagalaz.Web.App/src-admin/app/features/cache`, especially the sprite detail route, sprite manager, existing sprite stores/services, and focused tests.
- **Backend/API:** No changes. Reuse `GET /types/sprites/{id}`, `GET /types/sprites/{id}/image?frame=N`, and `POST /types/sprites/{id}/image`.
- **Dependencies:** No new packages.
- **Design:** Follow the existing Runic Material portal theme and the attached Sprite Studio reference.
