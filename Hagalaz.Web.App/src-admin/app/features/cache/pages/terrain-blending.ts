import type { TerrainLayer } from "../../../../../shared/model-viewer/terrain-material";
import { floorHsl, floorHslPaletteColor, floorPaletteColor } from "../../../../../shared/model-viewer/model-lighting";
import type { MapFloor, MapRenderRegion } from "../services/map-render.models";

export type FloorSample = { key: string; layer: TerrainLayer; weight: number };
export type VisibleFloor = MapFloor & { rgb: number };

export function isVisibleFloor(floor: MapFloor): floor is VisibleFloor {
    return floor.rgb !== null && floor.rgb !== 0xff00ff;
}

export function linearFloorColor(rgb: number): number[] {
    return linearColor(floorPaletteColor(rgb));
}

export function linearColor(channels: readonly number[]): number[] {
    return channels.map((channel) => {
        const value = channel / 255;
        return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
    });
}

/** Shared world coordinates give both sides of a selected-region seam identical samples. */
export function terrainSampler(regions: readonly MapRenderRegion[]): (plane: number, x: number, y: number) => FloorSample[] {
    const tiles = new Map<string, { key: string; layer: TerrainLayer; hsl: readonly number[] }>();
    for (const region of regions) {
        const floors = new Map<number, { key: string; layer: TerrainLayer; hsl: readonly number[] }>();
        for (const floor of region.floors) {
            if (floor.overlay || !isVisibleFloor(floor)) continue;
            floors.set(floor.id, {
                key: `${floor.id}:${floor.rgb}:${floor.textureId}:${floor.textureScale}`,
                hsl: floorHsl(floor.rgb),
                layer: { textureId: floor.textureId, textureScale: floor.textureScale, color: linearFloorColor(floor.rgb) },
            });
        }
        region.planes.forEach((plane, level) => {
            for (let tile = 0; tile < 4096; tile++) {
                const floor = floors.get(plane.underlays[tile]);
                if (floor) tiles.set(`${level}:${region.regionX * 64 + Math.floor(tile / 64)}:${region.regionY * 64 + (tile % 64)}`, { ...floor, layer: { ...floor.layer } });
            }
        });
    }
    // Class274's rolling window adds coordinate +5 and removes -5: ten tiles per axis.
    // Summed-area tables keep this linear in selected terrain size, including neighbour halos.
    for (const region of regions) region.planes.forEach((_, level) => {
        const stride = 74;
        const sums = Array.from({ length: 5 }, () => new Float64Array(stride * stride));
        for (let x = 0; x < 73; x++) for (let y = 0; y < 73; y++) {
            const tile = tiles.get(`${level}:${region.regionX * 64 + x - 4}:${region.regionY * 64 + y - 4}`);
            const at = (x + 1) * stride + y + 1;
            for (let component = 0; component < 5; component++) {
                const sum = sums[component];
                sum[at] = (tile ? component === 4 ? 1 : tile.hsl[component] : 0) + sum[at - stride] + sum[at - 1] - sum[at - stride - 1];
            }
        }
        for (let x = 0; x < 64; x++) for (let y = 0; y < 64; y++) {
            const tile = tiles.get(`${level}:${region.regionX * 64 + x}:${region.regionY * 64 + y}`);
            if (!tile) continue;
            const [hue, saturation, lightness, multiplier, count] = sums.map(sum => sum[(x + 10) * stride + y + 10] - sum[x * stride + y + 10] - sum[(x + 10) * stride + y] + sum[x * stride + y]);
            tile.layer.color = linearColor(floorHslPaletteColor(Math.trunc(hue * 256 / multiplier), Math.trunc(saturation / count), Math.trunc(lightness / count)));
        }
    });
    return (plane, x, y) => {
        const tx = Math.floor(x - 0.5),
            ty = Math.floor(y - 0.5);
        const fx = x - 0.5 - tx,
            fy = y - 0.5 - ty;
        const samples = new Map<string, FloorSample>();
        for (const [dx, dy, weight] of [
            [0, 0, (1 - fx) * (1 - fy)],
            [1, 0, fx * (1 - fy)],
            [0, 1, (1 - fx) * fy],
            [1, 1, fx * fy],
        ]) {
            const tile = tiles.get(`${plane}:${tx + dx}:${ty + dy}`);
            if (!tile || weight === 0) continue;
            const sample = samples.get(tile.key) ?? { key: tile.key, layer: { ...tile.layer, color: [0, 0, 0] }, weight: 0 };
            sample.layer.color = sample.layer.color.map((value, channel) => value + tile.layer.color[channel] * weight);
            sample.weight += weight;
            samples.set(tile.key, sample);
        }
        const total = [...samples.values()].reduce((sum, sample) => sum + sample.weight, 0);
        return [...samples.values()].map((sample) => ({ ...sample, layer: { ...sample.layer, color: sample.layer.color.map(value => value / sample.weight) }, weight: sample.weight / total }));
    };
}
