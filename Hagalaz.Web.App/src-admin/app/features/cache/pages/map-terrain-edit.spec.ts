import { describe, expect, it } from "vitest";
import type { MapFloor, MapRenderPlane, MapRenderRegion } from "../services/map-render.models";
import { required } from "../../../../testing/required";
import { applyBrushStroke, applyTerrainEdits, floorKey, footprintOutline, mapTerrainSaves, paintedFloorTextures, terrainBrushOutline, terrainEditNotes, terrainStrokePreview, type TerrainBrush, type TerrainEdits } from "./map-terrain-edit";

const plane = (height: number): MapRenderPlane => ({ cornerHeights: Array(4225).fill(height), flags: Array(4096).fill(0), underlays: Array(4096).fill(1), overlays: Array(4096).fill(0), overlayShapes: Array(4096).fill(0), overlayRotations: Array(4096).fill(0) });
const region = (regionX = 1, regionY = 2, heights = [-640, -1600, -2560, -3520]): MapRenderRegion => ({ id: (regionX << 8) | regionY, regionX, regionY, terrainStatus: "available", landscapeStatus: "available", planes: heights.map(plane), objects: [], floors: [], warnings: [] });
const brush = (change: Partial<TerrainBrush> = {}): TerrainBrush => ({ tool: "raise", plane: 0, size: 1, strength: 2, underlay: null, overlay: null, overlayShape: 0, overlayRotation: 0, ...change });
const key = (plane: number, x: number, y: number) => (plane << 12) | (x << 6) | y;
const at = (x: number, y: number) => ({ x: x * 512, z: y * 512 });
const corner = (x: number, y: number) => x * 65 + y;
const edits = (id: number, ...tiles: [number, object][]): TerrainEdits => new Map([[id, new Map(tiles)]]);
const none = new Map<string, MapFloor>();

describe("applyBrushStroke", () => {
    it("raises and lowers by the strength in steps of 32", () => {
        const start = region();
        const raised = applyBrushStroke([start], undefined, brush(), [at(3, 4)]);
        expect(raised?.get(start.id)?.get(key(0, 3, 4))).toEqual({ height: 22 });
        const lowered = applyBrushStroke([start], undefined, brush({ tool: "lower", strength: 4 }), [at(3, 4)]);
        expect(lowered?.get(start.id)?.get(key(0, 3, 4))).toEqual({ height: 16 });
    });

    it("keeps heights storable and above the plane below", () => {
        const flat = region(1, 2, [0, -960, -1920, -2880]);
        const up = applyBrushStroke([flat], undefined, brush({ strength: 1 }), [at(0, 0)]);
        expect(up?.get(flat.id)?.get(key(0, 0, 0))?.height).toBe(2);
        const down = applyBrushStroke([region(1, 2, [-64, -960, -1920, -2880])], undefined, brush({ tool: "lower", strength: 1 }), [at(0, 0)]);
        expect(down?.get(flat.id)?.get(key(0, 0, 0))?.height).toBe(0);
        const floor = applyBrushStroke([flat], undefined, brush({ tool: "lower", strength: 8 }), [at(0, 0)]);
        expect(floor?.get(flat.id)?.get(key(0, 0, 0))?.height).toBe(0);
        const ceiling = applyBrushStroke([region(1, 2, [-8160, -9120, -10080, -11040])], undefined, brush({ strength: 8 }), [at(0, 0)]);
        expect(ceiling?.get(flat.id)?.get(key(0, 0, 0))?.height).toBe(255);
    });

    it("rounds an automatic height to a storable value", () => {
        const auto = region(1, 2, [-650, -1610, -2570, -3530]);
        expect(applyBrushStroke([auto], undefined, brush(), [at(0, 0)])?.get(auto.id)?.get(key(0, 0, 0))?.height).toBe(22);
    });

    it("applies a stroke once per tile and fills gaps between samples", () => {
        const start = region();
        const stroke = applyBrushStroke([start], undefined, brush(), [at(1, 1), at(1, 1), at(4, 1)]);
        const tiles = stroke?.get(start.id);
        expect([...tiles?.keys() ?? []].sort((a, b) => a - b)).toEqual([key(0, 1, 1), key(0, 2, 1), key(0, 3, 1), key(0, 4, 1)]);
        expect(tiles?.get(key(0, 1, 1))).toEqual({ height: 22 });
    });

    it("covers the brush area and leaves unloaded regions alone", () => {
        const start = region();
        const stroke = applyBrushStroke([start], undefined, brush({ size: 3 }), [at(0, 0)]);
        expect(stroke?.size).toBe(1);
        expect(stroke?.get(start.id)?.size).toBe(4);
    });

    it("flattens to the height where the stroke began", () => {
        const start = region();
        start.planes[0].cornerHeights[corner(5, 5)] = -1280;
        const flat = applyBrushStroke([start], undefined, brush({ tool: "flatten", size: 3 }), [at(5, 5)])?.get(start.id);
        expect(flat?.size).toBe(9);
        for (const edit of flat?.values() ?? []) expect(edit.height).toBe(40);
    });

    it("paints and clears floors on the tile under the point", () => {
        const start = region();
        const point = { x: 3.9 * 512, z: 4.1 * 512 };
        expect(applyBrushStroke([start], undefined, brush({ tool: "underlay", underlay: 7 }), [point])?.get(start.id)?.get(key(0, 3, 4))).toEqual({ underlay: 7 });
        expect(applyBrushStroke([start], undefined, brush({ tool: "overlay", overlay: 9, overlayShape: 2, overlayRotation: 3 }), [point])?.get(start.id)?.get(key(0, 3, 4))).toEqual({ overlay: 9, overlayShape: 2, overlayRotation: 3 });
        expect(applyBrushStroke([start], undefined, brush({ tool: "clear-overlay" }), [point])?.get(start.id)?.get(key(0, 3, 4))).toEqual({ overlay: 0 });
    });

    it("returns the same edits when nothing applies", () => {
        const before = edits(region().id, [key(0, 0, 0), { height: 4 }]);
        expect(applyBrushStroke([region()], before, brush({ tool: "underlay" }), [at(1, 1)])).toBe(before);
        expect(applyBrushStroke([region()], before, brush(), [])).toBe(before);
        expect(applyBrushStroke([{ ...region(), terrainStatus: "failed" }], before, brush(), [at(1, 1)])).toBe(before);
    });

    it("merges with earlier edits to a tile", () => {
        const start = region();
        const merged = applyBrushStroke([start], edits(start.id, [key(0, 1, 1), { underlay: 3 }]), brush(), [at(1, 1)]);
        expect(merged?.get(start.id)?.get(key(0, 1, 1))).toEqual({ underlay: 3, height: 22 });
    });
});

describe("applyTerrainEdits", () => {
    it("keeps identity without edits and for regions they do not reach", () => {
        const start = [region(), region(9, 9)];
        expect(applyTerrainEdits(start, undefined, none)).toBe(start);
        expect(applyTerrainEdits(start, edits(start[0].id, [key(0, 5, 5), { height: 2 }]), none)[1]).toBe(start[1]);
    });

    it("sets a height and carries the planes above it without changing the source", () => {
        const start = region();
        const [preview] = applyTerrainEdits([start], edits(start.id, [key(0, 5, 5), { height: 30 }]), none);
        expect(preview.planes[0].cornerHeights[corner(5, 5)]).toBe(-960);
        expect(preview.planes[1].cornerHeights[corner(5, 5)]).toBe(-1920);
        expect(preview.planes[3].cornerHeights[corner(5, 5)]).toBe(-3840);
        expect(preview.planes[0].cornerHeights[corner(5, 6)]).toBe(-640);
        expect(start.planes[0].cornerHeights[corner(5, 5)]).toBe(-640);
    });

    it("applies a higher plane relative to the plane below", () => {
        const start = region();
        const [preview] = applyTerrainEdits([start], edits(start.id, [key(1, 5, 5), { height: 10 }]), none);
        expect(preview.planes[0].cornerHeights[corner(5, 5)]).toBe(-640);
        expect(preview.planes[1].cornerHeights[corner(5, 5)]).toBe(-960);
    });

    it("moves the underwater layer with plane 0", () => {
        const start = { ...region(), planes: [...region().planes, plane(-200)] };
        const [preview] = applyTerrainEdits([start], edits(start.id, [key(0, 5, 5), { height: 30 }]), none);
        expect(preview.planes[4].cornerHeights[corner(5, 5)]).toBe(-520);
    });

    it("updates shared corners of loaded neighbours from the owning region only", () => {
        const west = region(1, 2), east = region(2, 2);
        const [left, right] = applyTerrainEdits([west, east], edits(east.id, [key(0, 0, 5), { height: 30 }], [key(0, 7, 5), { height: 30 }]), none);
        expect(left.planes[0].cornerHeights[corner(64, 5)]).toBe(-960);
        expect(left.planes[0].cornerHeights[corner(7, 5)]).toBe(-640);
        expect(right.planes[0].cornerHeights[corner(0, 5)]).toBe(-960);
    });

    it("updates the far corner of a diagonal neighbour", () => {
        const start = region(1, 2), diagonal = region(2, 3);
        const [preview] = applyTerrainEdits([start, diagonal], edits(diagonal.id, [key(0, 0, 0), { height: 30 }]), none);
        expect(preview.planes[0].cornerHeights[corner(64, 64)]).toBe(-960);
    });

    it("skips corners without a known height", () => {
        const start = region();
        start.planes[0].cornerHeights[corner(5, 5)] = null;
        const [preview] = applyTerrainEdits([start], edits(start.id, [key(0, 5, 5), { height: 30 }]), none);
        expect(preview.planes[0].cornerHeights[corner(5, 5)]).toBeNull();
    });

    it("applies floors and adds definitions the region does not use", () => {
        const start = region();
        const floor: MapFloor = { id: 9, overlay: true, rgb: 1, textureId: 4, secondaryRgb: null, textureScale: 512 };
        const catalog = new Map([[floorKey(true, 9), floor]]);
        const [preview] = applyTerrainEdits([start], edits(start.id, [key(0, 1, 2), { underlay: 3, overlay: 9, overlayShape: 2, overlayRotation: 1 }]), catalog);
        const tile = 64 + 2;
        expect(preview.planes[0].underlays[tile]).toBe(3);
        expect(preview.planes[0].overlays[tile]).toBe(9);
        expect(preview.planes[0].overlayShapes[tile]).toBe(2);
        expect(preview.planes[0].overlayRotations[tile]).toBe(1);
        expect(preview.floors).toEqual([floor]);
        expect(start.floors).toEqual([]);
    });

    it("clears an overlay with its shape and rotation", () => {
        const start = region();
        start.planes[0].overlays[0] = 4;
        start.planes[0].overlayShapes[0] = 3;
        start.planes[0].overlayRotations[0] = 2;
        const [preview] = applyTerrainEdits([start], edits(start.id, [key(0, 0, 0), { overlay: 0 }]), none);
        expect([preview.planes[0].overlays[0], preview.planes[0].overlayShapes[0], preview.planes[0].overlayRotations[0]]).toEqual([0, 0, 0]);
    });
});

describe("mapTerrainSaves", () => {
    it("omits tiles and regions left equal to what is stored", () => {
        const start = region();
        expect(mapTerrainSaves([start], undefined)).toEqual([]);
        expect(mapTerrainSaves([start], edits(start.id, [key(0, 1, 1), { height: 20, underlay: 1, overlay: 0 }]))).toEqual([]);
    });

    it("sends only the values that changed", () => {
        const start = region();
        expect(mapTerrainSaves([start], edits(start.id, [key(0, 1, 1), { height: 22, underlay: 1 }], [key(1, 2, 3), { underlay: 5 }]))).toEqual([
            { kind: "terrain", regionId: start.id, tiles: [{ plane: 0, x: 1, y: 1, height: 22 }, { plane: 1, x: 2, y: 3, underlay: 5 }] },
        ]);
    });

    it("sends overlay shape and rotation with a painted overlay and not with a cleared one", () => {
        const start = region();
        start.planes[0].overlays[0] = 4;
        expect(mapTerrainSaves([start], edits(start.id, [key(0, 0, 0), { overlay: 0 }], [key(0, 0, 1), { overlay: 6, overlayShape: 1, overlayRotation: 2 }]))[0].tiles).toEqual([
            { plane: 0, x: 0, y: 0, overlay: 0 }, { plane: 0, x: 0, y: 1, overlay: 6, overlayShape: 1, overlayRotation: 2 },
        ]);
    });

    it("detects a changed shape or rotation on the same overlay", () => {
        const start = region();
        start.planes[0].overlays[0] = 4;
        expect(mapTerrainSaves([start], edits(start.id, [key(0, 0, 0), { overlay: 4, overlayShape: 0, overlayRotation: 0 }]))).toEqual([]);
        expect(mapTerrainSaves([start], edits(start.id, [key(0, 0, 0), { overlay: 4, overlayShape: 2, overlayRotation: 0 }]))[0].tiles).toEqual([{ plane: 0, x: 0, y: 0, overlay: 4, overlayShape: 2, overlayRotation: 0 }]);
    });
});

describe("terrainStrokePreview", () => {
    const tiles = (positions: readonly number[]) => positions.length / 18;

    it("draws a height stroke at the resulting height on every tile sharing the corner", () => {
        const preview = required(terrainStrokePreview([region()], brush(), [at(5, 5)], none), "the preview");
        expect(tiles(preview.positions)).toBe(4);
        expect(preview.positions).toContain(704);
        expect(preview.rgb).toEqual([0xffb800, 0xffb800, 0xffb800, 0xffb800]);
        expect(preview.opacity).toBeLessThan(1);
        expect(preview.textureId).toBeNull();
    });

    it("draws a painted floor's texture on the brushed tiles only", () => {
        const floor: MapFloor = { id: 9, overlay: true, rgb: 0, textureId: 44, secondaryRgb: null, textureScale: 1024 };
        const preview = required(terrainStrokePreview([region()], brush({ tool: "overlay", overlay: 9, size: 1 }), [{ x: 5.5 * 512, z: 5.5 * 512 }], new Map([[floorKey(true, 9), floor]])), "the preview");
        expect(tiles(preview.positions)).toBe(1);
        expect(preview.textureId).toBe(44);
        expect(preview.opacity).toBe(1);
        expect(preview.rgb).toEqual([0xa0a0a0]);
        expect(preview.positions.slice(0, 3)).toEqual([5 * 512, 640, 5 * 512]);
        expect(preview.uvs.slice(0, 2)).toEqual([34.5, -66.5]);
    });

    it("shows a solid floor in its colour", () => {
        const floor: MapFloor = { id: 3, overlay: false, rgb: 0x204060, textureId: -1, secondaryRgb: null, textureScale: 512 };
        expect(terrainStrokePreview([region()], brush({ tool: "underlay", underlay: 3 }), [{ x: 1.5 * 512, z: 1.5 * 512 }], new Map([[floorKey(false, 3), floor]]))?.rgb).toEqual([0x204060]);
    });

    it("shows the ground under a cleared overlay", () => {
        const start = { ...region(), floors: [{ id: 1, overlay: false, rgb: 0x112233, textureId: -1, secondaryRgb: null, textureScale: 512 }] };
        expect(terrainStrokePreview([start], brush({ tool: "clear-overlay" }), [{ x: 1.5 * 512, z: 1.5 * 512 }], none)?.rgb).toEqual([0x112233]);
    });

    it("has nothing to draw without points or a usable floor", () => {
        expect(terrainStrokePreview([region()], brush(), [], none)).toBeNull();
        expect(terrainStrokePreview([], brush(), [at(1, 1)], none)).toBeNull();
        expect(terrainStrokePreview([region()], brush({ tool: "underlay" }), [at(1, 1)], none)).toBeNull();
    });

    it("skips tiles without known corner heights", () => {
        const start = region();
        start.planes[0].cornerHeights[corner(5, 5)] = null;
        expect(tiles(terrainStrokePreview([start], brush({ tool: "clear-overlay" }), [{ x: 8.5 * 512, z: 8.5 * 512 }], none)?.positions ?? [])).toBe(1);
        expect(terrainStrokePreview([start], brush({ tool: "clear-overlay" }), [{ x: 5.5 * 512, z: 5.5 * 512 }], none)).toBeNull();
    });
});

describe("terrain edit helpers", () => {
    it("lists textures of painted floors once and in order", () => {
        const start = region();
        const floor = (id: number, textureId: number, overlay = false): MapFloor => ({ id, overlay, rgb: 1, textureId, secondaryRgb: null, textureScale: 512 });
        const catalog = new Map([floor(1, 8), floor(2, -1), floor(3, 5, true)].map(item => [floorKey(item.overlay, item.id), item]));
        expect(paintedFloorTextures(undefined, catalog)).toEqual([]);
        expect(paintedFloorTextures(edits(start.id, [1, { underlay: 1 }], [2, { underlay: 2 }], [3, { overlay: 3 }], [4, { underlay: 1 }]), catalog)).toEqual([5, 8]);
    });

    it("notes that flags stay as they are only for height edits", () => {
        const start = region();
        expect(terrainEditNotes(undefined)).toEqual([]);
        expect(terrainEditNotes(edits(start.id, [1, { underlay: 2 }]))).toEqual([]);
        expect(terrainEditNotes(edits(start.id, [1, { height: 2 }]))).toEqual(["Walkability and other tile flags are not changed."]);
    });

    it("outlines the brush at ground height in scene units", () => {
        const start = region();
        const tile = terrainBrushOutline([start], brush({ tool: "underlay" }), { x: 3.5 * 512, z: 4.5 * 512 });
        expect(tile?.slice(0, 3)).toEqual([3 * 512, 640, 4 * 512]);
        expect(tile).toHaveLength(4 * 3);
        const height = terrainBrushOutline([start], brush({ size: 3 }), at(5, 5));
        expect(height?.slice(0, 3)).toEqual([3.5 * 512, 640, 3.5 * 512]);
        expect(terrainBrushOutline([], brush(), at(5, 5))).toBeNull();
    });

    it("outlines an object's whole footprint from the tile under the pointer", () => {
        const outline = required(footprintOutline([region()], 0, { x: 3.5 * 512, z: 4.5 * 512 }, 2, 3), "the footprint outline");
        expect(outline).toHaveLength((2 + 3) * 2 * 3);
        expect(outline.slice(0, 3)).toEqual([3 * 512, 640, 4 * 512]);
        const xs = outline.filter((_, index) => index % 3 === 0), zs = outline.filter((_, index) => index % 3 === 2);
        expect([Math.min(...xs), Math.max(...xs)]).toEqual([3 * 512, 5 * 512]);
        expect([Math.min(...zs), Math.max(...zs)]).toEqual([4 * 512, 7 * 512]);
        expect(footprintOutline([], 0, at(5, 5), 2, 3)).toBeNull();
    });
});
