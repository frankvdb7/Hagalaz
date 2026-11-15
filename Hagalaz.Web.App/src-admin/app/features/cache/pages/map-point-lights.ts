import { pointLightRgb } from "../../../../../shared/model-viewer/model-lighting";
import type { MapLightField } from "../../../../../shared/model-viewer/map-light-material";
import type { MapRenderRegion } from "../services/map-render.models";

export function mapSceneOrigin(regions: readonly MapRenderRegion[]): { x: number; y: number } {
    return {
        x: Math.min(...regions.map(region => region.regionX)) * 64,
        y: Math.min(...regions.map(region => region.regionY)) * 64,
    };
}

type RegionLight = NonNullable<MapRenderRegion["pointLights"]>[number];

interface LightGrid {
    width: number;
    height: number;
    tiles: Float32Array;
}

/** Stores a light index in the first free of the tile and plane's four slots; a full tile keeps its first four lights. */
function claimSlot(tiles: Float32Array, offset: number, index: number) {
    for (let slot = offset; slot < offset + 4; slot++) {
        if (tiles[slot] !== 0) continue;
        tiles[slot] = index;
        return;
    }
}

/** Marks every tile and plane covered by the light's row spans, given the light's top-left tile in the scene. */
function markLightTiles({ width, height, tiles }: LightGrid, light: RegionLight, left: number, top: number, index: number) {
    const size = light.rows.length;
    for (const [row, span] of light.rows.entries()) {
        const y = top + row;
        if (y < 0 || y >= height) continue;
        const start = Math.min(span >> 8, size - 1);
        const end = Math.min(start + (span & 255), size);
        for (let x = Math.max(0, left + start); x < Math.min(width, left + end); x++) {
            for (let plane = light.minPlane; plane <= light.maxPlane; plane++) claimSlot(tiles, ((plane * height + y) * width + x) * 4, index);
        }
    }
}

/** Each tile and plane keeps up to four lights in cache order, as in the client. */
export function mapLightField(regions: readonly MapRenderRegion[]): MapLightField | null {
    if (!regions.some(region => region.pointLights?.length)) return null;
    const origin = mapSceneOrigin(regions);
    const width = (Math.max(...regions.map(region => region.regionX)) + 1) * 64 - origin.x;
    const height = (Math.max(...regions.map(region => region.regionY)) + 1) * 64 - origin.y;
    const grid: LightGrid = { width, height, tiles: new Float32Array(width * height * 4 * 4) };
    const lights: MapLightField["lights"][number][] = [];

    for (const region of regions) {
        const offsetX = region.regionX * 64 - origin.x;
        const offsetY = region.regionY * 64 - origin.y;
        for (const light of region.pointLights ?? []) {
            const tileX = Math.floor(light.x / 512);
            const tileY = Math.floor(light.y / 512);
            const ground = region.planes[light.plane]?.cornerHeights[tileX * 65 + tileY];
            if (ground == null) continue;
            lights.push({
                x: offsetX * 512 + light.x, y: -ground + light.height, z: offsetY * 512 + light.y,
                radius: light.radius, rgb: pointLightRgb(light.packedColor),
            });
            const radiusTiles = (light.rows.length - 1) / 2;
            markLightTiles(grid, light, offsetX + tileX - radiusTiles, offsetY + tileY - radiusTiles, lights.length);
        }
    }
    return lights.length ? { ...grid, lights } : null;
}
