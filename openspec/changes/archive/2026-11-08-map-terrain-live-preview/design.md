# Design

## Context

See proposal.md for the measurements. `buildMapScene` produces one geometry with terrain in the first vertices and objects after it, and `MapScenePreparation` caches terrain by region identity. A terrain edit creates new regions, so terrain and the combined object build both rerun. `ModelScene` holds one mesh; `setGeometry` rebuilds its buffers, material groups and normals. Selection uses vertex ranges within that buffer, and object drop limits picking to `startVertex` of the first placement. `ModelScene` also serves the single-model preview.

## Goals / Non-Goals

Goals and non-goals are in proposal.md. This design adds no worker and leaves the brush rules and saved data alone.

## Decisions

**A stroke overlay gives the instant feedback.** While a stroke is active, the page derives the brushed tiles with the existing `applyBrushStroke` on the stroke's points so far, and builds simple quads for those tiles only. Height strokes draw at the resulting height in a tinted, slightly offset surface. Floor strokes use the floor's texture when it is loaded and otherwise its colour. `ModelScene.setStrokePreview` draws them as a small separate mesh, like the brush outline. Work is proportional to brushed tiles, so it meets the frame budget regardless of scene size. Rejected: running the real terrain build each frame, which at 195 ms cannot reach it.

**The overlay is an approximation, by design.** It does not reproduce overlay spreading or terrain blending. The release draws the exact result, and the overlay is removed only after that scene is drawn, to avoid a flash of old ground.

**Terrain and objects become separate meshes.** `buildMapScene` returns separate terrain and object geometry, and placement vertex ranges become relative to the object geometry. `ModelScene` gains a second surface for map scenes; per-mesh state moves into one small class used for both, so the single-model path keeps one mesh. Picking uses the surface it needs, replacing the `startVertex` limit. Rejected: partial updates of a single buffer, because a floor change alters a tile's triangle count and shifts every object vertex after it.

**Object geometry is kept across paint strokes, and re-fitted selectively for height strokes.** Objects depend on heights, not floors, so the cache key for object geometry uses the regions' heights rather than region identity. For a height stroke, each placement's built vertices are retained, and only placements whose footprint touches a changed corner are rebuilt. Rejected: re-fitting every object on each height stroke, which keeps the 184 ms and the upload.

**Translucent ordering is kept explicit.** Surfaces are currently sorted together. With two meshes, terrain draws first and objects after, and blended object surfaces continue to sort among themselves. Water is terrain and draws under translucent objects, matching the usual view. Before and after screenshots of a water region decide whether this holds; a mismatch triggers the stop condition.

**State owners.** The viewport owns the live stroke points. The page owns the derived preview and the committed draft. `MapScenePreparation` remains the single cache owner. `ModelScene` owns GPU resources.

**Measuring.** Timings use the live editor on region 12850 at the largest brush, averaged over at least four strokes: overlay work per sample, then release to the next drawn frame, with the existing dev build.

## Results

Live on region 12850, with the existing dev build:

| Measure | Result | Target |
| --- | --- | --- |
| Overlay per pointer sample, height brush 7 by 7 | 2.5 ms average | under 16 ms |
| Overlay per pointer sample, paint brush 7 by 7 | 0.8 ms average | under 16 ms |
| Release to drawn, paint | 242 ms average | 250 ms |
| Release to drawn, height | 251 ms average | 350 ms |
| Before and after screenshots, wide and close, including water | 0% of pixels differ by more than 8 of 255 | no visible change |

The first overlay update of a session took 38 ms while its buffers were created. Normals are not recomputed for re-fitted objects; only reflective effect materials use them, and the height changes involved are small.

## Risks / Trade-offs

- [`setGeometry` is already the most complex method in the frontend] → Move state into the per-surface class without changing its logic first, and rely on the existing scene tests.
- [The overlay can differ slightly from the final ground, for example at blended edges] → Accepted as a preview; the release shows the exact result.
- [Objects do not follow the ground until release] → Accepted, and listed as a non-goal.
- [Translucent surfaces may overlap differently] → Compared by screenshot, with a stop condition.
- [A 3 by 3 selection remains slower on release] → Terrain is still built per scene; the overlay keeps the drag responsive.

## Migration Plan

No data migration or backend change. Rollback is reverting the frontend change.
