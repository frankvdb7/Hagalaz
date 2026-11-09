import { describe, expect, it } from "vitest";
import type { MapRenderRegion } from "../services/map-render.models";
import { mapLightField } from "./map-point-lights";

describe("map point lights", () => {
    const planes = Array.from({ length: 4 }, (_, plane) => ({
        cornerHeights: Array<number>(65 * 65).fill(-320 - plane * 960),
        flags: [], underlays: [], overlays: [], overlayShapes: [], overlayRotations: [],
    }));
    const region: MapRenderRegion = {
        id: 258, regionX: 1, regionY: 2, terrainStatus: "available", landscapeStatus: "absent",
        planes, floors: [], objects: [], warnings: [],
    };

    it("projects light height from its source plane and keeps tile coverage across a region seam", () => {
        const source = {
            ...region,
            pointLights: [{
                x: 63 * 512 + 256, y: 256, height: 192, radius: 768, packedColor: 0x03c0,
                plane: 1, minPlane: 1, maxPlane: 3, rows: [0x0101, 0x0003, 0x0101],
            }],
        };
        const field = mapLightField([source, { ...region, id: 514, regionX: 2 }])!;
        // Hardware HSL palette at fixed gamma 0.7.
        expect(field.lights).toEqual([{ x: 32512, y: 1472, z: 256, radius: 768, rgb: 0xfa2916 }]);
        const lightAt = (x: number, y: number, plane: number) => field.tiles[((plane * field.height + y) * field.width + x) * 4];
        expect(lightAt(64, 0, 1)).toBe(1);
        expect(lightAt(64, 0, 3)).toBe(1);
        expect(lightAt(64, 0, 0)).toBe(0);
        expect(lightAt(64, 1, 1)).toBe(0);
        expect(lightAt(63, 1, 1)).toBe(1);
    });

    it("leaves the field empty for older render responses", () => {
        expect(mapLightField([region])).toBeNull();
    });
});
