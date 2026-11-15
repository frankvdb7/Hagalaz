import type { MapOverlayProperties, MapRenderRegion } from "../services/map-render.models";
import { linearColor } from "./terrain-blending";

export interface WaterFog {
    color: readonly number[];
    scale: number;
    bias: number;
}

/** The client records fog from each tile's surface overlay, so the seabed reads plane 0's overlay. */
export function waterFogLookup(regions: readonly MapRenderRegion[]): (worldX: number, worldY: number) => WaterFog | undefined {
    const fogs = new Map<MapRenderRegion, Map<number, WaterFog>>();
    return (worldX, worldY) => {
        const region = regions.find(candidate => worldX >= candidate.regionX * 64 && worldX < candidate.regionX * 64 + 64 && worldY >= candidate.regionY * 64 && worldY < candidate.regionY * 64 + 64);
        if (!region?.planes[0]) return undefined;
        let overlays = fogs.get(region);
        if (!overlays) {
            overlays = regionFogs(region);
            fogs.set(region, overlays);
        }
        return overlays.get(region.planes[0].overlays[(worldX - region.regionX * 64) * 64 + worldY - region.regionY * 64]);
    };
}

function regionFogs(region: MapRenderRegion): Map<number, WaterFog> {
    const fogs = new Map<number, WaterFog>();
    for (const floor of region.floors) if (floor.overlay && floor.overlayProperties) fogs.set(floor.id, waterFog(floor.overlayProperties));
    return fogs;
}

function waterFog(properties: MapOverlayProperties): WaterFog {
    const rgb = properties.waterFogRgb;
    return { color: linearColor([(rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255]), scale: properties.waterFogScale, bias: properties.waterFogBias / 255 };
}

/** Class49_Sub2 ground program: the vertex's texture Z is depth - 1 in legacy height units. */
export function waterFogFactor(fog: WaterFog, depth: number): number {
    return Math.min(Math.max(((depth - 1) * 256) / fog.scale + fog.bias, 0), 1);
}

export function mixWaterFog(color: readonly number[], fog: WaterFog, factor: number): number[] {
    return color.map((channel, index) => channel + (fog.color[index] - channel) * factor);
}
