## Context

`Atmosphere.decodeGeneric` reads flag `0x20` as a full int sky colour (default `0xC8C0A8`) and flag `0x40` as an unsigned short fog value (default `0`). When no skybox is selected the scene pass clears colour and depth to that sky colour. `AtmosphereManager.method4583` sends the same colour as the OpenGL fog colour with parameter `(fogValue + 256) << 2`. `GraphicsToolkit_Sub1.method4830` sets linear fog `end = far` and `start = max(near, end - parameter)`. `RegionManager.method3006` sets `far = floor(34.46 * 104) << 2 = 14332` scene units for the standard 104-tile scene.

## Decisions

- Keep the raw fog value on the API (`fogValue`), as the rendering contract requires. The browser derives the client parameter and range. Do not rename it to a density.
- Sky and fog belong to the region environment. They apply only while region atmosphere lighting is active; default lighting keeps the existing surface background and no fog.
- Fog is off by default and is a viewport toggle. The editor's fitted overview camera sits beyond the client's far plane, so client-range fog would hide the whole map until the user zooms in.
- Use Three.js linear `Fog`, which also measures eye depth, with the 14332-unit client far plane as fog end. The optional toolkit `+512` far-plane extension is not modelled.
- Procedural skyboxes are not decoded or drawn. Regions with a skybox therefore show the flat sky colour.
