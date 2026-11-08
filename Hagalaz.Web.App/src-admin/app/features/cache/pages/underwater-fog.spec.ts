import { describe, expect, it } from "vitest";
import type { MapFloor, MapOverlayProperties, MapRenderRegion } from "../services/map-render.models";
import { mixWaterFog, waterFogFactor, waterFogLookup } from "./underwater-fog";

const properties: MapOverlayProperties = { priority: 8, spreadsToNeighbours: false, waterFogRgb: 0xff0000, waterFogScale: 512, waterFogBias: 0, waterParameter20: 63, waterParameter21: 0, waterParameter22: 64 };
const floor: MapFloor = { id: 3, overlay: true, rgb: 1, textureId: -1, secondaryRgb: null, textureScale: 512, overlayProperties: properties };
function region(id: number): MapRenderRegion {
    const overlays = Array(4096).fill(0);
    overlays[5 * 64 + 6] = 3;
    return { id, regionX: id >> 8, regionY: id & 255, terrainStatus: "available", landscapeStatus: "available", objects: [], warnings: [], floors: [floor], planes: [{ cornerHeights: [], flags: [], underlays: [], overlays, overlayShapes: [], overlayRotations: [] }] };
}

describe("underwater fog", () => {
    it("ramps with depth from the bias and saturates", () => {
        const fog = { color: [1, 0, 0], scale: 512, bias: 0 };
        expect(waterFogFactor(fog, 1)).toBe(0);
        expect(waterFogFactor(fog, 2)).toBe(0.5);
        expect(waterFogFactor(fog, 400)).toBe(1);
        expect(waterFogFactor({ ...fog, bias: 1 }, 0)).toBe(0.5);
        expect(waterFogFactor({ ...fog, bias: 1 }, 1)).toBe(1);
        expect(waterFogFactor(fog, 0)).toBe(0);
    });
    it("reads the surface overlay of the tile's own region and ignores tiles without one", () => {
        const lookup = waterFogLookup([region(258), region(514)]);
        const fog = lookup(2 * 64 + 5, 2 * 64 + 6);
        expect(fog).toEqual({ color: [1, 0, 0], scale: 512, bias: 0 });
        expect(lookup(1 * 64 + 4, 2 * 64 + 6)).toBeUndefined();
        expect(lookup(0, 0)).toBeUndefined();
    });
    it("blends linearly towards the fog colour", () => {
        expect(mixWaterFog([0, 1, 0], { color: [1, 0, 0], scale: 512, bias: 0 }, 0.25)).toEqual([0.25, 0.75, 0]);
    });
});
