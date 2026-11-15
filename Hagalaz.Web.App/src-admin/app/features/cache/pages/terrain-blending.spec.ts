import { describe, expect, it } from "vitest";
import { terrainSampler } from "./terrain-blending";
import type { MapRenderRegion } from "../services/map-render.models";

function region(x: number, rgb: number, textureId: number): MapRenderRegion {
    return {
        id: x << 8,
        regionX: x,
        regionY: 0,
        terrainStatus: "available",
        landscapeStatus: "absent",
        objects: [],
        warnings: [],
        floors: [{ id: 1, overlay: false, rgb, textureId, textureScale: 512, secondaryRgb: null }],
        planes: [{ cornerHeights: [], flags: [], underlays: Array(4096).fill(1), overlays: [], overlayShapes: [], overlayRotations: [] }],
    };
}

describe("terrain material sampling", () => {
    it("crossfades neighbouring textures at a shared region edge without changing tile centers", () => {
        const left = region(0, 0xff0000, 10),
            right = region(1, 0x0000ff, 20);
        right.floors[0].textureScale = 1024;
        const sample = terrainSampler([left, right]);
        expect(sample(0, 63.5, 10.5).map((value) => [value.layer.textureId, value.weight])).toEqual([[10, 1]]);
        expect(sample(0, 64, 10.5).map((value) => [value.layer.textureId, value.weight])).toEqual([
            [10, 0.5],
            [20, 0.5],
        ]);
        expect(sample(0, 64.5, 10.5).map((value) => [value.layer.textureId, value.weight])).toEqual([[20, 1]]);
        expect(sample(0, 64, 10.5)[1].layer.textureScale).toBe(1024);
        expect(terrainSampler([right, left])(0, 64, 10.5)).toEqual(sample(0, 64, 10.5));
    });
    it("normalizes available neighbours and excludes other planes, missing tiles and transparent colours", () => {
        const source = region(0, 0x00ff00, -1);
        const sample = terrainSampler([source]);
        expect(sample(0, 0, 0)).toHaveLength(1);
        expect(sample(0, 0, 0)[0]).toMatchObject({ weight: 1, layer: { color: [0.01599629336550963, 0.8796223968878317, 0.021219010376003555] } });
        expect(sample(1, 1, 1)).toEqual([]);
        expect(sample(0, 1000, 1000)).toEqual([]);
        source.floors[0].rgb = 0xff00ff;
        expect(terrainSampler([source])(0, 1, 1)).toEqual([]);
    });
    it("keeps texture weights while surrounding earth changes an isolated grass tint", () => {
        const source = region(50, 0x1c1813, 929);
        source.regionY = 50;
        source.floors.push({ id: 2, overlay: false, rgb: 0x29380f, textureId: 918, textureScale: 512, secondaryRgb: null });
        source.planes[0].underlays[1] = 2;
        const samples = terrainSampler([source])(0, 3200.5, 3201);
        expect(samples.map(sample => [sample.layer.textureId, sample.weight])).toEqual([[929, 0.5], [918, 0.5]]);
        const grass = samples[1].layer.color;
        expect(grass[1]).toBeLessThan(0.03);
        expect(grass[0]).toBeGreaterThan(grass[1]);
    });
    it("matches the original client neighbourhood golden beside the region 12850 bridge", () => {
        const source = region(50, 0, -1);
        source.regionY = 50;
        source.planes[0].underlays.fill(0);
        // Real 10x10 neighbourhood at local (40,26), m50_50: component sums
        // [779,14146,4694,5546,100], client packed HSL 8727 -> palette RGB [101,91,44].
        const definitions = [
            [10, 2629656, 14], [37, 6573598, 26], [49, 5793803, 3],
            [65, 6638859, 22], [160, 2701327, 29], [164, 1841171, 6],
        ];
        source.floors = definitions.map(([id, rgb]) => ({ id, rgb, overlay: false, textureId: -1, textureScale: 512, secondaryRgb: null }));
        const ids = definitions.flatMap(([id, , count]) => Array(count).fill(id));
        ids.forEach((id, index) => { source.planes[0].underlays[(6 + Math.floor(index / 10)) * 64 + 6 + index % 10] = id; });
        const samples = terrainSampler([source])(0, 3210.5, 3210.5);
        expect(samples).toHaveLength(1);
        const expected = [101,91,44].map(channel => ((channel / 255 + 0.055) / 1.055) ** 2.4);
        expected.forEach((value, channel) => { expect(samples[0].layer.color[channel]).toBeCloseTo(value, 10); });
        // Unrelated source planes must not alter the neighbourhood average.
        source.planes.push({ ...source.planes[0], underlays: Array(4096).fill(160) });
        expect(terrainSampler([source])(0, 3210.5, 3210.5)).toEqual(samples);
    });
});
