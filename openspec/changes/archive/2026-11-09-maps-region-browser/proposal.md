# Proposal

## Why

The Maps tab currently offers only an exact definition lookup. Its API returns terrain dimensions and object placements, but not the heights and floors needed to display a map. Administrators need to browse all available map regions in a table, then open a region's detail view to see real terrain and landscape objects for that region or several adjacent regions.

## What Changes

One concrete goal: provide a read-only Maps table-to-detail flow for browsing available normal cache regions and viewing a bounded selection in 3D.

- Show a paginated table of all available map regions at `/cache/types?kind=maps`, consistent with the other type browsers. Include region ID, X/Y coordinates, separate terrain/landscape file presence and an Open map action. Each region appears once even when both cache files exist; absent region IDs are not invented as table rows.
- Make the region ID a keyboard-accessible link to `/cache/types/maps/:id`. Replace that route's generic definition detail with the 3D map detail view and load the selected region automatically. Preserve table search and pagination when returning.
- Default the detail scene to one region; allow a rectangular selection of up to 3 by 3 adjacent regions from that anchor. Show the selected region IDs and coordinates. Table browsing does not load terrain, landscape, models, textures or a 3D viewport.
- Include both terrain `mX_Y` and landscape/object placements `lX_Y`. Provide four-plane visibility, landscape visibility, orbit/pan/zoom, and reset-to-selection controls.
- Add a server-side render projection preserving heights, floor identities, shapes, rotations, flags, and placement semantics. Keep gameplay-facing map data unchanged.
- Resolve real floor appearance and static object models through existing cache, model, texture and Material UI mechanisms. Report unavailable regions, encrypted landscape data, and unsupported appearances without generating substitute geometry.
- Accept optional XTEA keys for selected regions as an advanced input. Keep keys local to the current workspace and out of URLs and persistent browser storage.
- Preserve table context and the detail's applied region selection in the URL. Cancel obsolete loads and release scene resources on replacement or navigation.

### Non-goals

No tile/object picking or inspector within the map detail, map editing, cache writes, dynamic/instanced chunk assembly, whole-world streaming, gameplay simulation, object animation, or new atmosphere/water/shadow effects. The region table and 3D map detail are both in scope. Static default object appearances are in scope; live varp-driven variants are not.

### Acceptance criteria

- A1: Maps opens a paginated table of all regions present in the cache, with region IDs, coordinates and terrain/landscape file presence. Clicking a region ID or Open map navigates to its detail route and automatically loads real terrain and available landscape objects; returning restores table context. The list performs no scene decoding or rendering.
- A2: Selections from 1 by 1 through 3 by 3 place regions in one scene at their true relative coordinates, with shared terrain edges and no wraparound at the region-coordinate boundary.
- A3: Terrain uses authoritative heights and decoded floors on all four planes. Ordinary walls, decorations, and static objects use the correct shape, orientation, footprint, definition transforms and required terrain contouring. Representative real-cache fixtures prove these inputs.
- A4: Missing terrain, missing landscape, incorrect/missing keys, unsupported object appearances and request failures are distinguishable. Valid regions and terrain remain inspectable when a landscape or neighboring region is unavailable, and retry does not duplicate objects.
- A5: Table context, detail identity and applied dimensions survive refresh and history navigation. Direct detail links load without visiting the table. Invalid or oversized selections make no render request. Stale requests cannot replace a newer scene; leaving or replacing the scene disposes its owned resources. Keyboard and phone-width controls remain usable.
- A6: Focused decoder/API, store, pure scene-assembly and component regressions pass, changed frontend coverage is measured, and the production admin build, project lint/audit, strict OpenSpec validation and desktop/phone browser checks are recorded.

### Stop conditions

Do not substitute generated terrain, guessed floor colors or placeholder object models for missing semantic data. If required revision-742 semantics or fixture provenance cannot be established from the existing rendering references and GameClient evidence, report the precise gap before changing scope. Stop before adding tile/object inspection, a live key service, new persistence, worker or streaming system.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `web-3d-map-rendering`: add the admin region table and map detail flow, bounded multi-region scene, static landscape assembly, partial-load feedback and navigation/resource lifecycle requirements. Reuse its existing authoritative terrain, floor, coordinate and parity requirements.

## Impact

- `Hagalaz.Cache.Abstractions` and `Hagalaz.Cache`: additive render-focused terrain/floor/placement projection and codec tests. Preserve `IMapType.TerrainData`, gameplay collision and existing `MapProvider` callers.
- `Hagalaz.Services.Cache`: an authorized metadata-only region listing API, additive map-render API, per-region availability and XTEA handling, and the object rendering metadata needed for placement. Reuse existing reference tables, `ICacheAPI`, model decoding/assembly and texture endpoints rather than creating another model pipeline.
- `Hagalaz.Web.App/src-admin`: paginated Maps integration in the existing Types route, the existing Maps detail route upgraded to a 3D view, page-scoped NgRx stores, region controls and a viewport using installed Three.js and the established admin theme.
- `Hagalaz.Web.App/shared/model-viewer`: reuse proven model preparation, material and texture semantics where applicable; keep region assembly separate from the single-model inspection lifecycle.
- Existing `docs/3d-rendering/` references and `web-3d-map-rendering` are the design authority. GameClient source/fixtures supply development evidence only, never a production runtime dependency. No new external package or live GameWorld dependency is planned.
