# Design

## Context

See proposal.md for motivation. Relevant current state:

- `GET /types/items/{id}/image` reads `wwwroot/images/items/{id}.gif` through the web root file provider.
- `ModelEndpoints.GetItemModel` already assembles an item's inventory model for the 3D viewer through `AssembleModel`: it decodes parts with `ModelGeometryDecoder`, applies scale, recolours and retextures, and computes texture coordinates with `ModelTextureCoordinates`. It returns an HTTP result, so other code cannot reuse it.
- `TextureImageDecoder` decodes cache textures with their alpha modes; ImageSharp is already referenced by the cache libraries.
- Client lighting (`lightModel`) and the HSL colour palette are ported only in the TypeScript model viewer. No .NET rasteriser exists.
- The reference client draws inventory icons in `ItemDefinition.method6279`, called through `World.method6168`. It renders into a 36×32 buffer with the projection centre at (16, 16), a 512 focal length and near plane 50; it rotates by `zan2d` about Z and `modelRotation2` about Y, translates using `modelZoom`, `modelRotation1` and the two offsets, then rotates by `modelRotation1` about X. Lighting uses ambient `64 + ambient`, contrast `768 + contrast`, sun direction (-50, -10, -50) and an intensity randomly chosen in 0.95–1.05. Outline passes (`method6284`) and the shadow pass (`method6285`) run on the pixel buffer; template drawables are composited before or after the item.

## Goals / Non-Goals

**Goals:**
- One owner for inventory-model assembly, shared by the 3D model endpoint and the icon renderer.
- A pure, deterministic renderer that can be tested without HTTP.

**Non-Goals:**
- Reusing the TypeScript viewer on the server, GPU rendering or headless browsers.
- A general-purpose .NET 3D renderer; only the client icon path is implemented.

## Decisions

### Render on every request, ETag from the PNG bytes
The service renders, encodes the PNG and derives a strong `ETag` from a SHA-256 of the bytes. A matching `If-None-Match` returns 304 after rendering, which saves the transfer. A 36×32 render of a typical item model is small CPU work, and admin tables request tens of icons at a time.
- *Rejected: output cache keyed by URL.* It would need invalidation on definition, model, texture and raw writes, a second mechanism for a cheap computation.
- *Rejected: ETag from a hash of the inputs.* It must enumerate every contributing definition, model and texture; missing one gives stale icons. Hashing the output is correct by construction.

### Extract inventory-model assembly from `ModelEndpoints`
`AssembleModel`'s decoding, scaling, recolouring, retexturing and texture-coordinate work moves into a method that returns assembled model data (or throws the existing format exceptions). The model endpoint maps that data to `GetModelDto`, so its responses do not change; the renderer consumes the same data. This keeps one owner for client assembly rules.
- *Rejected: decoding the model again inside the renderer.* It would duplicate recolour, retexture, scale and version rules.

### Renderer in `Hagalaz.Cache`, PNG encoding in the service
A static renderer in `Hagalaz.Cache/Logic/Rendering` takes assembled model data, the item's icon settings, decoded textures and render options, and returns RGBA pixels. The endpoint resolves templates, decodes textures, calls the renderer and encodes the PNG with ImageSharp. The renderer has no cache or HTTP dependencies, so golden tests run on plain inputs.

### Client camera, transform order and lighting
The renderer reproduces the client constants above: zoom ×1.5 for the note's inner item, ×1.04 at outline level 2, otherwise ×1; the same rotation and translation order; client lighting with ambient `64 + ambient` and contrast `768 + contrast`, sun (-50, -10, -50). The client's random light intensity is replaced by the centre of its range, 1.0, so renders are deterministic. Lighting and the HSL palette are ported to .NET from the same client routines as the TypeScript viewer; the server and browser are separate runtimes, so they cannot share code.

### Rasterisation
Faces are projected with the client perspective, culled like the client, and filled with scanline triangles. Opaque faces use a depth buffer; translucent faces are drawn afterwards, back to front, blending with face alpha. Colours use client flat or Gouraud shading per face. Textured faces sample the decoded texture with perspective-correct coordinates from `ModelTextureCoordinates` and nearest-neighbour filtering, multiplied by the face's lighting, honouring the texture's alpha mode. Pixels end fully opaque or fully transparent before the outline and shadow passes, as in the client.
- *Rejected: the client's painter's-algorithm ordering without a depth buffer.* At icon sizes a depth buffer gives the same visible result for ordinary models and avoids porting the client's priority bucketing; translucent faces keep painter's ordering.

### Templates
Noted items draw the note template, then the original item rendered with zoom ×1.5, outline 1 and no shadow on top, as in the client. Lent and bought items draw the original item first and the template over it. Template definitions resolve through the item type provider, which already applies the template copying in `ItemType`.

### Options and scaling
`width` and `height` default to 36 and 32 and are limited to 8–512. The renderer projects at a uniform scale of `min(width / 36, height / 32)` with the projection centre scaled and the result centred, so the client framing is preserved. Outline and shadow passes run once at the output resolution, one pixel wide. `outline` accepts 0, 1 (client colour `0xFF000002`) or 2 (then an outer `0xFFFFFFFF` pass). `shadow` accepts `none` or an `RRGGBB` colour; the default is the inventory shadow `302020`. Options are part of the rendered bytes, so they naturally produce distinct ETags.

### Remove the GIFs
The `wwwroot/images/items` directory and the web root lookup are deleted in this change. Rewriting existing branch history to purge the blobs is a separate repository operation and is not part of this change.

## Risks / Trade-offs

- [Textured or translucent faces differ slightly from the client at 36×32] → Golden tests pin the renderer's own output for representative items; a reviewer compares them visually with client screenshots before archiving.
- [Default outline and shadow may not match a specific interface] → Defaults follow the standard inventory component; the parameters allow callers to override them.
- [512×512 renders cost more CPU] → The size limit bounds the work per request; the default path is 36×32.
- [Rendering on every request] → Acceptable for admin traffic; revisit with measurements before adding any cache.

## Migration Plan

Deploy the cache service; the admin item results thumbnail already scales any image and treats failures as unavailable. Rollback restores the previous service build; the GIFs remain recoverable from git history.

## Open Questions

None. The default outline and shadow were confirmed against the backpack's CS2 script 1613 in the pinned cache (see tasks.md, 4.2).
