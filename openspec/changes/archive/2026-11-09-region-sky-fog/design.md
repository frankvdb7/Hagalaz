## Context

`Atmosphere.decodeGeneric` reads flag `0x20` as a full int sky colour (default `0xC8C0A8`) and flag `0x40` as an unsigned short fog value (default `0`). When no skybox is selected the scene pass clears colour and depth to that sky colour. `AtmosphereManager.method4583` sends the same colour as the OpenGL fog colour with parameter `(fogValue + 256) << 2`. `GraphicsToolkit_Sub1.method4830` sets linear fog `end = far` and `start = max(near, end - parameter)`. `RegionManager.method3006` sets `far = floor(34.46 * 104) << 2 = 14332` scene units for the standard 104-tile scene.

## Decisions

- Keep the raw fog value on the API (`fogValue`), as the rendering contract requires. The browser derives the client parameter and range. Do not rename it to a density.
- Sky and fog belong to the region environment. They apply only while region atmosphere lighting is active; default lighting keeps the existing surface background and no fog.
- Fog is off by default and is a viewport toggle. The editor's fitted overview camera sits beyond the client's far plane, so client-range fog would hide the whole map until the user zooms in.
- Use Three.js linear `Fog`, which also measures eye depth, with the 14332-unit client far plane as fog end. The optional toolkit `+512` far-plane extension is not modelled.
- Flag `0x80` carries six cache texture IDs. `EnvironmentMap_Sub2_Sub1` uploads them unchanged to `GL_TEXTURE_CUBE_MAP_POSITIVE_X + i`, so the API keeps that order (`+X, -X, +Y, -Y, +Z, -Z`); the client's local "top/bottom/front" argument names are not reliable.
- The map loader fetches the anchor region's faces through its existing texture queue but publishes them separately from surface textures, once all six exist as the client requires. A face image enters the surface collection only when a floor or object also uses it. `ModelScene` re-flips the 2D-oriented bitmaps to source row order and enlarges smaller faces without filtering.
- Only effects 7, 8 and 9 read the region cube (`GraphicsToolkit_Sub1.method4832`). Effect 1 (`Class49_Sub7`) binds three generated internal cubes instead, so it keeps the procedural sky until those are ported.
- Effects 8 and 9 reflect a world-oriented view vector directly into the cube, so the viewer samples client-space `(x, -y, z)`. Effect 7 looks the cube up by surface normal in the client; the viewer still uses reflection there. Existing blend weights stay.
- Procedural skyboxes are not decoded or drawn. Regions with a skybox therefore show the flat sky colour.
