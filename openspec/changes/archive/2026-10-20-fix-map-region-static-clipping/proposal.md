## Why

Static map objects decoded from the cache currently lose their 8x8 chunk origin when a complete 64x64 region is loaded. Their collision flags are therefore written to incorrect tiles, while database-backed custom objects continue to use their absolute coordinates.

## What Changes

- Decode static cache objects for a complete region using their full local X/Y coordinates.
- Load static cache objects on all four map planes.
- Preserve the existing chunk-oriented decoder contract for dynamic map-part use.
- Add regression tests for later 8x8 chunks, non-zero planes, and terrain callbacks.

### Non-goals

- Do not change database-backed custom object loading or collision writers.
- Do not change asynchronous region-load scheduling in this change.
- Do not change pathfinding algorithms, collision flag definitions, cache formats, or client protocol messages.

## Capabilities

### New Capabilities

- `map-region-static-clipping`: Static cache map objects and terrain must produce collision data at their actual region coordinates and planes.

### Modified Capabilities

- None.

## Impact

- `Hagalaz.Cache.Abstractions/Types/Providers/IMapProvider.cs`
- `Hagalaz.Cache/Types/Providers/MapProvider.cs`
- `Hagalaz.Services.GameWorld/Data/MapRegionLoader.cs`
- `Hagalaz.Cache.Tests/MapProviderTests.cs`
- No new dependencies or persisted-data changes.
