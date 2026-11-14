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

type BlendTile = { key: string; layer: TerrainLayer; hsl: readonly number[] };
type BlendTiles = ReadonlyMap<string, BlendTile>;

const sumStride = 74;

/** A tile's contribution to one summed component: hue, saturation, lightness, multiplier, then a count of 1. */
function blendComponent(tile: BlendTile | undefined, component: number): number {
    if (!tile) return 0;
    return component === 4 ? 1 : tile.hsl[component];
}

/** Summed-area tables over the region plane and its four-tile halo, one per blended component. */
function blendSums(tiles: BlendTiles, region: MapRenderRegion, level: number): Float64Array[] {
    const sums = Array.from({ length: 5 }, () => new Float64Array(sumStride * sumStride));
    for (let x = 0; x < 73; x++) for (let y = 0; y < 73; y++) {
        const tile = tiles.get(`${level}:${region.regionX * 64 + x - 4}:${region.regionY * 64 + y - 4}`);
        const at = (x + 1) * sumStride + y + 1;
        for (const [component, sum] of sums.entries()) {
            sum[at] = blendComponent(tile, component) + sum[at - sumStride] + sum[at - 1] - sum[at - sumStride - 1];
        }
    }
    return sums;
}

/** Recolours each underlay tile of the region plane with the average of its ten-by-ten window. */
function blendRegionPlane(tiles: BlendTiles, region: MapRenderRegion, level: number): void {
    const sums = blendSums(tiles, region, level);
    const windowSum = (sum: Float64Array, x: number, y: number) =>
        sum[(x + 10) * sumStride + y + 10] - sum[x * sumStride + y + 10] - sum[(x + 10) * sumStride + y] + sum[x * sumStride + y];
    for (let x = 0; x < 64; x++) for (let y = 0; y < 64; y++) {
        const tile = tiles.get(`${level}:${region.regionX * 64 + x}:${region.regionY * 64 + y}`);
        if (!tile) continue;
        const [hue, saturation, lightness, multiplier, count] = sums.map(sum => windowSum(sum, x, y));
        tile.layer.color = linearColor(floorHslPaletteColor(Math.trunc(hue * 256 / multiplier), Math.trunc(saturation / count), Math.trunc(lightness / count)));
    }
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
    for (const region of regions) {
        for (const level of region.planes.keys()) blendRegionPlane(tiles, region, level);
    }
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
