import { describe, expect, it } from "vitest";
import type { MapFloor, MapRenderRegion } from "../services/map-render.models";
import { overlayPriority, overlaySpreads, spreadSample } from "./overlay-spreading";

const overlayFloor = (id: number, priority: number, spreads = true): MapFloor => ({
    id, overlay: true, rgb: 0x0000ff + id, textureId: -1, secondaryRgb: null, textureScale: 512,
    overlayProperties: { priority, spreadsToNeighbours: spreads, waterFogRgb: 0x122b3d, waterFogScale: 512, waterFogBias: 255, waterParameter20: 63, waterParameter21: 0, waterParameter22: 64 },
});
function region(floors: MapFloor[], tiles: [x: number, y: number, floor: number, shape?: number, rotation?: number][]): MapRenderRegion {
    const plane = { cornerHeights: [], flags: [], underlays: [], overlays: Array(4096).fill(0), overlayShapes: Array(4096).fill(0), overlayRotations: Array(4096).fill(0) };
    for (const [x, y, floor, shape = 0, rotation = 0] of tiles) {
        plane.overlays[x * 64 + y] = floor;
        plane.overlayShapes[x * 64 + y] = shape;
        plane.overlayRotations[x * 64 + y] = rotation;
    }
    return { id: 0, regionX: 0, regionY: 0, terrainStatus: "available", landscapeStatus: "available", planes: [plane], objects: [], floors, warnings: [] };
}
const claimed = (points: readonly unknown[]) => [0, 1, 2, 3, 4, 5, 6, 7].filter(point => points[point] !== undefined);

describe("overlay spreading", () => {
    it("claims a shared edge from an orthogonal neighbour and one corner from a diagonal", () => {
        const spreads = overlaySpreads([region([overlayFloor(1, 8)], [[6, 5, 1]])]);
        expect(claimed(spreads(0, 5, 5))).toEqual([2, 3, 4]);
        const diagonal = overlaySpreads([region([overlayFloor(1, 8)], [[6, 6, 1]])]);
        expect(claimed(diagonal(0, 5, 5))).toEqual([4]);
    });
    it("ignores overlays that do not spread", () => {
        expect(claimed(overlaySpreads([region([overlayFloor(1, 8, false)], [[6, 5, 1]])])(0, 5, 5))).toEqual([]);
    });
    it("respects the neighbour's rotated shape coverage", () => {
        // Shape 3 covers local points 2-5 only: facing east it misses the west edge shared with x-1.
        expect(claimed(overlaySpreads([region([overlayFloor(1, 8)], [[6, 5, 1, 3, 0]])])(0, 5, 5))).toEqual([]);
        // Rotated twice those points face west.
        expect(claimed(overlaySpreads([region([overlayFloor(1, 8)], [[6, 5, 1, 3, 2]])])(0, 5, 5))).toEqual([2, 3, 4]);
    });
    it("gives a shared point to the higher authored priority, then the higher definition", () => {
        const floors = [overlayFloor(1, 9), overlayFloor(2, 8), overlayFloor(3, 8)];
        const high = overlaySpreads([region(floors, [[6, 5, 1], [5, 4, 2]])])(0, 5, 5);
        expect(high[2]?.sample.key).toBe("overlay:1");
        const tied = overlaySpreads([region(floors, [[6, 5, 3], [5, 4, 2]])])(0, 5, 5);
        expect(tied[2]?.sample.key).toBe("overlay:3");
        expect(overlayPriority(floors[0])).toBe((9 << 8) | 0);
    });
    it("only overrides a surface when the spread outranks it and maps rotated shape points", () => {
        const floors = [overlayFloor(1, 9), overlayFloor(2, 8)];
        const points = overlaySpreads([region(floors, [[6, 5, 1]])])(0, 5, 5);
        expect(spreadSample(points, 2, 0, overlayPriority(floors[1]))?.key).toBe("overlay:1");
        expect(spreadSample(points, 2, 0, overlayPriority(floors[0]))).toBeUndefined();
        // Local point 4 lands on world point 2 once rotated once.
        expect(spreadSample(points, 4, 1, -1)?.key).toBe("overlay:1");
        expect(spreadSample(points, 12, 0, -1)).toBeUndefined();
    });
});
