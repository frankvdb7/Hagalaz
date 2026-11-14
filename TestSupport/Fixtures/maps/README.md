# Region rendering fixture

`terrain-12850.dat` is the uncompressed `m50_50` payload from the pinned revision-742 cache. Its provenance records the source repository commit, archive identity, verified extracted data-file hash and raw terrain hash.

The expected semantic hash was captured by invoking the existing GameClient `Class274.readLandscapeData` method for all four planes and 64-by-64 tiles, with absolute tile coordinates. The capture hashes six signed big-endian int32 values per tile, in plane/X/Y order: height, flags, underlay ID, overlay ID, overlay shape and overlay rotation. Byte-valued client fields are converted to unsigned integers before hashing.

The C# test compares its full terrain projection against that independently captured hash. Normal test runs use only the committed payload and constant expected hash. They do not start Java, open an external cache or access the network. Updating the fixture requires a deliberate new independent client capture, not regenerating expectations with the C# decoder.

`landscape-12850.dat` contains the matching uncompressed `l50_50` payload. An independent Java capture uses the existing client's `Buffer.readIncrementor`, `readUnsignedSmart` and `readUnsignedByte` with the traversal in `Map.readObjectsFromLandscape`, before clipping/bridge adjustments. It records 5934 placements, hashing ID, shape, rotation, local X, local Y and source plane as big-endian int32 values in source order. This validates source placement decoding; it does not invoke the graphics-dependent full Map method or establish pixel parity for every object model. Payload and semantic hashes are in `provenance.json`.

Object collision definitions were captured on 2026-11-12 from the local Source/Cache object definition index (16) through ObjectTypeProvider. MapRenderDecoder decoded the pinned landscape to select the 800 distinct placed IDs before bridge adjustment. The JSON stores each base definition's sizeX, sizeY, clipType, solid and gateway fields, ordered by ID.

collision-12850.json was captured from MapRegionLoader and MapRegion using the pinned terrain, landscape, and base definitions. It stores all collision values in plane order, with each plane in local X then local Y order. The capture includes writes within region 12850 and excludes writes to unloaded neighbours.
