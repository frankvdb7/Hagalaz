import { describe, expect, it } from "vitest";
import { mapAppearanceKey, type MapPlacement, type MapRenderPlane, type MapRenderRegion, type ObjectRenderAppearance } from "../services/map-render.models";
import { required } from "../../../../testing/required";
import { applyTerrainEdits } from "./map-terrain-edit";
import { tileFlag, tileFlagOptions } from "./map-tile-flags";
import { buildWalkabilityOverlay, ineffectiveBlockRgb, objectCollisionRgb } from "./map-walkability";

const plane = (height: number): MapRenderPlane => ({ cornerHeights: Array(4225).fill(height), flags: Array(4096).fill(0), underlays: Array(4096).fill(1), overlays: Array(4096).fill(0), overlayShapes: Array(4096).fill(0), overlayRotations: Array(4096).fill(0) });
const region = (regionX = 1, regionY = 2): MapRenderRegion => ({ id: (regionX << 8) | regionY, regionX, regionY, terrainStatus: "available", landscapeStatus: "available", planes: [-640, -1600, -2560, -3520].map(plane), objects: [], floors: [], warnings: [] });
const flag = (target: MapRenderRegion, level: number, x: number, y: number, value: number) => { target.planes[level].flags[x * 64 + y] = value; };
const all = [true, true, true, true];
const rgbOf = (name: string) => required(tileFlagOptions.find(option => option.name === name), name).rgb;
const placement = (change: Partial<MapPlacement> = {}): MapPlacement => ({ id: 7, shape: 10, rotation: 0, x: 4, y: 5, plane: 0, ...change });
const appearance = (change: Partial<ObjectRenderAppearance> = {}): ObjectRenderAppearance => ({ id: 7, shape: 10, rotation: 0, sizeX: 1, sizeY: 1, decorDisplacement: 0, contourMode: 0, contourParameter: 0, parts: [], warnings: [], clipType: 2, solid: true, gateway: false, ...change });
const withObject = (object: MapPlacement, look: Partial<ObjectRenderAppearance> = {}) => {
    const target = region();
    target.objects.push(object);
    return { target, objects: { appearances: new Map([[mapAppearanceKey(object), appearance({ shape: object.shape, rotation: object.rotation, ...look })]]) } };
};
/** Each marker as its colour and the tile-space rectangle it covers. */
const markers = (overlay: ReturnType<typeof buildWalkabilityOverlay>) => (overlay?.rgb ?? []).map((rgb, index) => {
    const xs = [0, 1, 2, 3, 4, 5].map(vertex => required(overlay, "overlay").positions[(index * 6 + vertex) * 3] / 512);
    const zs = [0, 1, 2, 3, 4, 5].map(vertex => required(overlay, "overlay").positions[(index * 6 + vertex) * 3 + 2] / 512);
    const round = (value: number) => Math.round(value * 100) / 100;
    return { rgb, x0: round(Math.min(...xs)), y0: round(Math.min(...zs)), x1: round(Math.max(...xs)), y1: round(Math.max(...zs)) };
});

describe("buildWalkabilityOverlay", () => {
    it("draws nothing for unflagged regions or no regions", () => {
        expect(buildWalkabilityOverlay([], all)).toBeNull();
        expect(buildWalkabilityOverlay([region()], all)).toBeNull();
    });

    it("leaves a tile holding only the unnamed bit unmarked", () => {
        const target = region();
        flag(target, 0, 3, 4, 32);
        expect(buildWalkabilityOverlay([target], all)).toBeNull();
    });

    it("marks each flagged tile in its flag colour at ground height", () => {
        const target = region();
        flag(target, 0, 3, 4, tileFlag.blocked);
        flag(target, 0, 5, 6, tileFlag.roof);
        flag(target, 1, 7, 8, tileFlag.bridge);
        flag(target, 2, 9, 9, tileFlag.drawOnGroundPlane);
        flag(target, 3, 1, 1, tileFlag.hiddenOnMinimap);
        const overlay = required(buildWalkabilityOverlay([target], all), "overlay");
        expect(markers(overlay)).toEqual([
            { rgb: rgbOf("blocked"), x0: 3, y0: 4, x1: 4, y1: 5 },
            { rgb: rgbOf("roof"), x0: 5, y0: 6, x1: 6, y1: 7 },
            { rgb: rgbOf("bridge"), x0: 7, y0: 8, x1: 8, y1: 9 },
            { rgb: rgbOf("drawOnGroundPlane"), x0: 9, y0: 9, x1: 10, y1: 10 },
            { rgb: rgbOf("hiddenOnMinimap"), x0: 1, y0: 1, x1: 2, y1: 2 },
        ]);
        expect(overlay.positions.slice(0, 3)).toEqual([3 * 512, 640, 4 * 512]);
        expect(overlay.positions[19]).toBe(640);
        expect(overlay.positions[(2 * 6) * 3 + 1]).toBe(1600);
        expect(overlay.textureId).toBeNull();
        expect(overlay.opacity).toBeLessThan(1);
        expect(overlay.uvs).toHaveLength(overlay.rgb.length * 12);
    });

    it("follows a sloped tile", () => {
        const target = region();
        flag(target, 0, 0, 0, tileFlag.blocked);
        target.planes[0].cornerHeights[65] = -768;
        const overlay = required(buildWalkabilityOverlay([target], all), "overlay");
        expect([1, 4, 7].map(index => overlay.positions[index])).toEqual([640, 768, 640]);
    });

    it("shows a second flag as an inset and keeps blocked in front", () => {
        const target = region();
        flag(target, 0, 2, 2, tileFlag.roof | tileFlag.blocked | tileFlag.hiddenOnMinimap);
        expect(markers(buildWalkabilityOverlay([target], all))).toEqual([
            { rgb: rgbOf("blocked"), x0: 2, y0: 2, x1: 3, y1: 3 },
            { rgb: rgbOf("roof"), x0: 2.3, y0: 2.3, x1: 2.7, y1: 2.7 },
        ]);
    });

    it("omits hidden planes and tiles without ground", () => {
        const target = region();
        flag(target, 0, 1, 1, tileFlag.blocked);
        flag(target, 1, 2, 2, tileFlag.blocked);
        flag(target, 1, 10, 10, tileFlag.blocked);
        target.planes[1].cornerHeights[10 * 65 + 10] = null;
        expect(markers(buildWalkabilityOverlay([target], [false, true, true, true]))).toEqual([{ rgb: rgbOf("blocked"), x0: 2, y0: 2, x1: 3, y1: 3 }]);
        expect(buildWalkabilityOverlay([target], [false, false, false, false])).toBeNull();
    });

    it("dims blocked ground that a plane 1 bridge makes ineffective", () => {
        const target = region();
        flag(target, 0, 6, 6, tileFlag.blocked);
        flag(target, 1, 6, 6, tileFlag.bridge | tileFlag.blocked);
        const drawn = markers(buildWalkabilityOverlay([target], all));
        expect(drawn.map(marker => marker.rgb)).toEqual([ineffectiveBlockRgb, rgbOf("blocked"), rgbOf("bridge")]);
    });

    it("positions markers relative to the south-west region", () => {
        const east = region(2, 2);
        flag(east, 0, 0, 0, tileFlag.blocked);
        expect(markers(buildWalkabilityOverlay([region(1, 2), east], all))[0]).toMatchObject({ x0: 64, y0: 0 });
    });

    it("follows unsaved flag edits", () => {
        const target = region();
        const edited = applyTerrainEdits([target], new Map([[target.id, new Map([[(2 << 6) | 3, { flags: tileFlag.blocked }]])]]), new Map());
        expect(buildWalkabilityOverlay([target], all)).toBeNull();
        expect(markers(buildWalkabilityOverlay(edited, all))).toEqual([{ rgb: rgbOf("blocked"), x0: 2, y0: 3, x1: 3, y1: 4 }]);
    });
});

describe("object collision in the overlay", () => {
    it("covers a rotated footprint", () => {
        const { target, objects } = withObject(placement({ rotation: 1 }), { sizeX: 2, sizeY: 1 });
        expect(markers(buildWalkabilityOverlay([target], all, objects))).toEqual([
            { rgb: objectCollisionRgb, x0: 4, y0: 5, x1: 5, y1: 6 },
            { rgb: objectCollisionRgb, x0: 4, y0: 6, x1: 5, y1: 7 },
        ]);
        const unrotated = withObject(placement(), { sizeX: 2, sizeY: 1 });
        expect(markers(buildWalkabilityOverlay([unrotated.target], all, unrotated.objects)).map(marker => marker.x0)).toEqual([4, 5]);
    });

    it("leaves non-blocking, unloaded and unknown objects unmarked", () => {
        const open = withObject(placement(), { clipType: 0 });
        expect(buildWalkabilityOverlay([open.target], all, open.objects)).toBeNull();
        const legacy = withObject(placement(), { clipType: undefined, gateway: undefined });
        expect(buildWalkabilityOverlay([legacy.target], all, legacy.objects)).toBeNull();
        const pending = withObject(placement());
        expect(buildWalkabilityOverlay([pending.target], all, { appearances: new Map() })).toBeNull();
        expect(buildWalkabilityOverlay([pending.target], all)).toBeNull();
        const decoration = withObject(placement({ shape: 4 }));
        expect(buildWalkabilityOverlay([decoration.target], all, decoration.objects)).toBeNull();
    });

    it("marks a gateway even without a clip type", () => {
        const gate = withObject(placement({ shape: 0 }), { clipType: 0, gateway: true });
        expect(markers(buildWalkabilityOverlay([gate.target], all, gate.objects))).toHaveLength(1);
    });

    it("marks a ground decoration only for clip type 1", () => {
        const blocking = withObject(placement({ shape: 22 }), { clipType: 1 });
        expect(markers(buildWalkabilityOverlay([blocking.target], all, blocking.objects))).toEqual([{ rgb: objectCollisionRgb, x0: 4, y0: 5, x1: 5, y1: 6 }]);
        const walkable = withObject(placement({ shape: 22 }), { clipType: 2 });
        expect(buildWalkabilityOverlay([walkable.target], all, walkable.objects)).toBeNull();
    });

    it.each([
        [0, 0, [{ x0: 4, y0: 5, x1: 4.14, y1: 6 }]],
        [0, 1, [{ x0: 4, y0: 5.86, x1: 5, y1: 6 }]],
        [0, 2, [{ x0: 4.86, y0: 5, x1: 5, y1: 6 }]],
        [0, 3, [{ x0: 4, y0: 5, x1: 5, y1: 5.14 }]],
        [1, 0, [{ x0: 4, y0: 5.75, x1: 4.25, y1: 6 }]],
        [1, 1, [{ x0: 4.75, y0: 5.75, x1: 5, y1: 6 }]],
        [3, 2, [{ x0: 4.75, y0: 5, x1: 5, y1: 5.25 }]],
        [3, 3, [{ x0: 4, y0: 5, x1: 4.25, y1: 5.25 }]],
        [2, 0, [{ x0: 4, y0: 5, x1: 4.14, y1: 6 }, { x0: 4, y0: 5.86, x1: 5, y1: 6 }]],
        [2, 2, [{ x0: 4.86, y0: 5, x1: 5, y1: 6 }, { x0: 4, y0: 5, x1: 5, y1: 5.14 }]],
    ])("marks the edges of wall shape %i at rotation %i", (shape, rotation, edges) => {
        const wall = withObject(placement({ shape, rotation }));
        expect(markers(buildWalkabilityOverlay([wall.target], all, wall.objects))).toEqual(edges.map(edge => ({ rgb: objectCollisionRgb, ...edge })));
    });

    it("drops ground objects under a bridge and keeps the ones on it", () => {
        const { target, objects } = withObject(placement());
        flag(target, 1, 4, 5, tileFlag.bridge);
        expect(buildWalkabilityOverlay([target], all, objects)?.rgb ?? []).not.toContain(objectCollisionRgb);
        const upper = withObject(placement({ plane: 1 }));
        flag(upper.target, 1, 4, 5, tileFlag.bridge);
        expect(buildWalkabilityOverlay([upper.target], all, upper.objects)?.rgb).toContain(objectCollisionRgb);
    });

    it("uses edited placements, hides objects on hidden planes and skips unloaded neighbours", () => {
        const { target, objects } = withObject(placement({ x: 63 }), { sizeX: 2 });
        expect(markers(buildWalkabilityOverlay([target], all, objects))).toHaveLength(1);
        expect(buildWalkabilityOverlay([target], [false, true, true, true], objects)).toBeNull();
        const moved = { ...objects, placements: new Map([[target.id, [placement({ x: 10 })]]]) };
        expect(markers(buildWalkabilityOverlay([target], all, moved)).map(marker => marker.x0)).toEqual([10, 11]);
        const underwater = withObject(placement({ plane: 4 }));
        expect(buildWalkabilityOverlay([underwater.target], [true, true, true, true, true], underwater.objects)).toBeNull();
    });
});
