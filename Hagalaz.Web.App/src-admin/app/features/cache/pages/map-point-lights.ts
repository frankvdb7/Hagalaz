import { pointLightRgb } from "../../../../../shared/model-viewer/model-lighting";
import type { MapLightField } from "../../../../../shared/model-viewer/map-light-material";
import type { MapRenderRegion } from "../services/map-render.models";

export function mapSceneOrigin(regions: readonly MapRenderRegion[]): { x: number; y: number } {
    return {
        x: Math.min(...regions.map(region => region.regionX)) * 64,
        y: Math.min(...regions.map(region => region.regionY)) * 64,
    };
}

/** Each tile and plane keeps up to four lights in cache order, as in the client. */
export function mapLightField(regions: readonly MapRenderRegion[]): MapLightField | null {
    if (!regions.some(region => region.pointLights?.length)) return null;
    const origin = mapSceneOrigin(regions);
    const width = (Math.max(...regions.map(region => region.regionX)) + 1) * 64 - origin.x;
    const height = (Math.max(...regions.map(region => region.regionY)) + 1) * 64 - origin.y;
    const tiles = new Float32Array(width * height * 4 * 4);
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
            const left = offsetX + tileX - radiusTiles;
            const top = offsetY + tileY - radiusTiles;
            for (const [row, span] of light.rows.entries()) {
                const y = top + row;
                if (y < 0 || y >= height) continue;
                const start = Math.min(span >> 8, light.rows.length - 1);
                const end = Math.min(start + (span & 255), light.rows.length);
                for (let x = Math.max(0, left + start); x < Math.min(width, left + end); x++) {
                    for (let plane = light.minPlane; plane <= light.maxPlane; plane++) {
                        const offset = ((plane * height + y) * width + x) * 4;
                        for (let slot = offset; slot < offset + 4; slot++) {
                            if (tiles[slot] !== 0) continue;
                            tiles[slot] = lights.length;
                            break;
                        }
                    }
                }
            }
        }
    }
    return lights.length ? { width, height, tiles, lights } : null;
}
