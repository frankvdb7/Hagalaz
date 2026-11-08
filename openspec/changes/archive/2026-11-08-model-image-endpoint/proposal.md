## Why

Only items can be rendered to images, because only items have an authored icon camera. Raw cached models cannot be previewed as images.

## What Changes

- Add `GET /models/{id}/image`, rendering any model to a transparent PNG with an automatically fitted camera.
- Add `GET /types/objects/{id}/image` for an object shape with its definition's scale and recolours.
- Show those previews in the map editor's object catalog, replacing the flat in-scene canvas thumbnail.
- Share the item icon rasteriser, PNG encoding and revalidation.

## Capabilities

### New Capabilities

- `cache-model-images`: PNG previews of cached models.

## Impact

`ItemIconRenderer`, the item icon endpoint's response handling and the model endpoints. NPC images, and previews in the Models tab, are out of scope.
