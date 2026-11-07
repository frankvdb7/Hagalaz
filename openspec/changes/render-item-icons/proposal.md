# Proposal

## Why

Item sprites are served from 33,418 committed GIF files (78 MB) that are a snapshot of the cache. Duplicated items have no sprite, edited models or recolours keep showing the old picture, and the binaries bloat every clone. The cache already contains everything the client uses to draw an inventory icon, so the service can render it on demand.

## Goal

Render each item's inventory icon from the cache on request, the way the 742 client draws it, and stop shipping pre-rendered GIFs.

## What Changes

- **BREAKING** `GET /types/items/{id}/image` returns a PNG rendered from the item definition, its inventory model, recolours, retextures, scale and 2D icon settings instead of a static GIF.
- Defaults reproduce the client's inventory icon: 36×32 pixels, client camera and lighting, outline level 1 and the inventory drop shadow. Noted, lent and bought items are composited on their template the way the client does.
- Optional query parameters customise the output: `width` and `height` (the icon scales with the requested size), `outline` (0, 1 or 2, matching the client's outline levels) and `shadow` (an RGB colour or `none`).
- **BREAKING** Responses use `Cache-Control: private, no-cache` with a content-based `ETag` and answer matching `If-None-Match` with 304, replacing `public, max-age=86400` and `Last-Modified`.
- Remove the committed GIFs under `Hagalaz.Services.Cache/wwwroot/images/items` and the static-file lookup.

## In scope

- One shared inventory-model assembly path used by both the existing item model endpoint and the icon renderer.
- A deterministic software rasteriser for the client icon path: perspective projection, client model lighting, flat and Gouraud colour, alpha, textured faces, depth test, outline and shadow passes.
- Validation of the new query parameters and a size limit.

## Non-goals

- Stack-count text, quantity-based stack model variants and player-appearance recolours.
- Equipped, head or animated renders, and any 3D preview change.
- Matching the existing GIFs pixel for pixel: they are not client output (32×32, no outline or shadow). They remain only a visual reference during review.
- Server-side image caching, CDN headers or background pre-rendering.
- Frontend changes beyond what the new size needs; the item results thumbnail already scales any image.

## Acceptance criteria

1. A default request for a typical item returns a 36×32 PNG whose framing, colours, outline and shadow follow the client routine. Pinned renders for a fixed set of items (plain, textured, translucent, rotated/offset, scaled, noted) are reproduced exactly by the test suite.
2. `width`/`height`, `outline` and `shadow` change the output as specified; invalid values return 400 with a validation message.
3. Items without an inventory model, missing models and unsupported model formats return 404 or 422 problem responses instead of an image.
4. Repeat requests return the same bytes and `ETag`; a matching `If-None-Match` returns 304. Editing the item definition or its model changes the `ETag`.
5. A duplicated item renders an icon immediately.
6. The item model endpoint behaviour is unchanged after sharing the assembly path.
7. The GIF files and static lookup are removed; Cache Studio item results show the rendered icons.

## Stop conditions

Pause and propose a follow-up instead of expanding this change if the client icon path needs features outside the list above (for example particle effects, animated textures or HD-only materials), or if exact client lighting requires state not present in the cache.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `cache-item-images`: item images are rendered from the cache with client defaults and optional size, outline and shadow parameters, with content-based revalidation instead of a static GIF.

## Impact

- Cache service: item image endpoint, model assembly in `ModelEndpoints`, new rendering code in `Hagalaz.Cache`, tests.
- Repository: deletes `Hagalaz.Services.Cache/wwwroot/images/items` (33,418 files).
- Reuses: `ModelGeometryDecoder`, `ModelTextureCoordinates`, `TextureImageDecoder`, item codecs, ImageSharp (already referenced) and the existing cache authorization. No new dependencies.
