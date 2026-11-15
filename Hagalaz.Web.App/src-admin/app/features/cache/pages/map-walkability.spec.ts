import { describe, expect, it } from "vitest";
import type { MapPlacement, MapRenderPlane, MapRenderRegion, ObjectCollision } from "../services/map-render.models";
import { required } from "../../../../testing/required";
import { applyTerrainEdits } from "./map-terrain-edit";
import { tileFlag, tileFlagOptions } from "./map-tile-flags";
import { buildCollisionMap } from "./map-collision";
import { buildWalkabilityOverlay, ineffectiveBlockRgb, objectCollisionRgb, projectileObjectRgb, projectileWallRgb, walkableRgb } from "./map-walkability";

const plane = (height: number): MapRenderPlane => ({ cornerHeights: Array(4225).fill(height), flags: Array(4096).fill(0), underlays: Array(4096).fill(0), overlays: Array(4096).fill(0), overlayShapes: Array(4096).fill(0), overlayRotations: Array(4096).fill(0) });
const region = (regionX = 1, regionY = 2): MapRenderRegion => ({ id: (regionX << 8) | regionY, regionX, regionY, terrainStatus: "available", landscapeStatus: "available", planes: [-640, -1600, -2560, -3520].map(plane), objects: [], floors: [], warnings: [] });
const flag = (target: MapRenderRegion, level: number, x: number, y: number, value: number) => { target.planes[level].flags[x * 64 + y] = value; };
const all = [true, true, true, true];
const rgbOf = (name: string) => required(tileFlagOptions.find(option => option.name === name), name).rgb;
const placement = (change: Partial<MapPlacement> = {}): MapPlacement => ({ id: 7, shape: 10, rotation: 0, x: 4, y: 5, plane: 0, ...change });
const definition: ObjectCollision = { sizeX: 1, sizeY: 1, clipType: 2, solid: false, gateway: false };
const draw = (
    regions: readonly MapRenderRegion[], visiblePlanes: readonly boolean[],
    definitions = new Map<number, ObjectCollision>(),
    edits = new Map<number, MapPlacement[]>(),
) => buildWalkabilityOverlay(regions, visiblePlanes, buildCollisionMap(regions, edits, definitions));
/** Each marker as its colour and the tile-space rectangle it covers. */
const markers = (overlay: ReturnType<typeof buildWalkabilityOverlay>) => (overlay?.rgb ?? []).map((rgb, index) => {
    const xs = [0, 1, 2, 3, 4, 5].map(vertex => required(overlay, "overlay").positions[(index * 6 + vertex) * 3] / 512);
    const zs = [0, 1, 2, 3, 4, 5].map(vertex => required(overlay, "overlay").positions[(index * 6 + vertex) * 3 + 2] / 512);
    const round = (value: number) => Math.round(value * 100) / 100;
    return { rgb, x0: round(Math.min(...xs)), y0: round(Math.min(...zs)), x1: round(Math.max(...xs)), y1: round(Math.max(...zs)) };
});

describe("buildWalkabilityOverlay", () => {
    it("draws nothing for regions without floors or flags or for no regions", () => {
        expect(draw([], all)).toBeNull();
        expect(draw([region()], all)).toBeNull();
    });

    it("marks walkable floors in the overlay and keeps wall edges distinct", () => {
        const target = region();
        const { underlays, overlays } = target.planes[0];
        underlays[0] = underlays[64] = underlays[128] = 1;
        overlays[192] = 1;
        flag(target, 0, 1, 0, tileFlag.blocked);
        flag(target, 0, 3, 0, tileFlag.roof);
        target.objects = [placement({ x: 2, y: 0 }), placement({ shape: 0, rotation: 1, x: 0, y: 0 })];
        const definitions = new Map([[7, definition]]);
        const movement = markers(draw([target], all, definitions));
        expect(movement.filter(marker => marker.rgb === walkableRgb)).toEqual([
            { rgb: walkableRgb, x0: 0.06, y0: 0.06, x1: 0.94, y1: 0.94 },
            { rgb: walkableRgb, x0: 3.18, y0: 0.18, x1: 3.3, y1: 0.3 },
        ]);
        expect(movement).toContainEqual({ rgb: objectCollisionRgb, x0: 0, y0: 0.86, x1: 1, y1: 1 });
        expect(draw([target], [false, true, true, true], definitions)).toBeNull();
    });

    it("leaves a tile holding only the unnamed bit unmarked", () => {
        const target = region();
        flag(target, 0, 3, 4, 32);
        expect(draw([target], all)).toBeNull();
    });

    it("marks each flagged tile in its flag colour at ground height", () => {
        const target = region();
        flag(target, 0, 3, 4, tileFlag.blocked);
        flag(target, 0, 5, 6, tileFlag.roof);
        flag(target, 1, 7, 8, tileFlag.bridge);
        flag(target, 2, 9, 9, tileFlag.drawOnGroundPlane);
        flag(target, 3, 1, 1, tileFlag.hiddenOnMinimap);
        const overlay = required(draw([target], all), "overlay");
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

    it("follows the terrain triangles on a non-planar tile", () => {
        const target = region();
        flag(target, 0, 0, 0, tileFlag.blocked);
        target.planes[0].cornerHeights[65] = -768;
        const overlay = required(draw([target], all), "overlay");
        expect(overlay.positions.slice(0, 9)).toEqual([0, 640, 0, 256, 672, 256, 512, 768, 0]);
        expect(overlay.positions).toHaveLength(4 * 9);
        expect(overlay.uvs).toHaveLength(overlay.rgb.length * 12);
    });

    it("shows a second flag as an inset and keeps blocked in front", () => {
        const target = region();
        flag(target, 0, 2, 2, tileFlag.roof | tileFlag.blocked | tileFlag.hiddenOnMinimap);
        expect(markers(draw([target], all))).toEqual([
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
        expect(markers(draw([target], [false, true, true, true]))).toEqual([{ rgb: rgbOf("blocked"), x0: 2, y0: 2, x1: 3, y1: 3 }]);
        expect(draw([target], [false, false, false, false])).toBeNull();
    });

    it("dims blocked ground that a plane 1 bridge makes ineffective", () => {
        const target = region();
        flag(target, 0, 6, 6, tileFlag.blocked);
        flag(target, 1, 6, 6, tileFlag.bridge | tileFlag.blocked);
        const drawn = markers(draw([target], all));
        expect(drawn.map(marker => marker.rgb)).toEqual([ineffectiveBlockRgb, rgbOf("blocked"), rgbOf("blocked"), rgbOf("bridge")]);
    });

    it("positions markers relative to the south-west region", () => {
        const east = region(2, 2);
        flag(east, 0, 0, 0, tileFlag.blocked);
        expect(markers(draw([region(1, 2), east], all))[0]).toMatchObject({ x0: 64, y0: 0 });
    });

    it("follows unsaved flag edits", () => {
        const target = region();
        const edited = applyTerrainEdits([target], new Map([[target.id, new Map([[(2 << 6) | 3, { flags: tileFlag.blocked }]])]]), new Map());
        expect(draw([target], all)).toBeNull();
        expect(markers(draw(edited, all))).toEqual([{ rgb: rgbOf("blocked"), x0: 2, y0: 3, x1: 3, y1: 4 }]);
    });
});

describe("object collision in the overlay", () => {
    it("marks both sides of a wall across a region edge", () => {
        const west = region(), east = region(2, 2);
        east.objects = [placement({ shape: 0, x: 0 })];
        expect(markers(draw([west, east], all, new Map([[7, definition]])))).toEqual([
            { rgb: objectCollisionRgb, x0: 63.86, y0: 5, x1: 64, y1: 6 },
            { rgb: objectCollisionRgb, x0: 64, y0: 5, x1: 64.14, y1: 6 },
        ]);
    });

    it("marks a model-less object and follows an unsaved placement move", () => {
        const target = region();
        target.objects = [placement({ rotation: 1 })];
        const definitions = new Map([[7, { ...definition, sizeX: 2 }]]);
        expect(markers(draw([target], all, definitions))).toEqual([
            { rgb: objectCollisionRgb, x0: 4, y0: 5, x1: 5, y1: 6 },
            { rgb: objectCollisionRgb, x0: 4, y0: 6, x1: 5, y1: 7 },
        ]);
        const edits = new Map([[target.id, [placement({ x: 10 })]]]);
        expect(markers(draw([target], all, definitions, edits)).map(marker => marker.x0)).toEqual([10, 11]);
    });

    it("distinguishes movement-only blockers from blockers that also stop shots", () => {
        const target = region();
        flag(target, 0, 1, 1, tileFlag.blocked);
        target.objects = [
            placement(), placement({ shape: 0, x: 12 }),
            placement({ id: 8, x: 16 }), placement({ id: 8, shape: 0, x: 20 }),
        ];
        const definitions = new Map([[7, { ...definition, solid: true }], [8, definition]]);
        const drawn = markers(draw([target], all, definitions));
        expect(drawn.filter(marker => marker.rgb === projectileObjectRgb || marker.rgb === projectileWallRgb)).toEqual([
            { rgb: projectileObjectRgb, x0: 4, y0: 5, x1: 5, y1: 6 },
            { rgb: projectileWallRgb, x0: 11.86, y0: 5, x1: 12, y1: 6 },
            { rgb: projectileWallRgb, x0: 12, y0: 5, x1: 12.14, y1: 6 },
        ]);
        expect(drawn.filter(marker => marker.rgb === objectCollisionRgb)).toHaveLength(3);
        expect(drawn).toHaveLength(7);
    });
});
