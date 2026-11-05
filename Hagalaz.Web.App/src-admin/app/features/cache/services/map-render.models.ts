import type { GetModelDto } from "./dtos";

export interface MapRenderPlane {
    cornerHeights: (number | null)[];
    flags: number[];
    underlays: number[];
    overlays: number[];
    overlayShapes: number[];
    overlayRotations: number[];
}
export interface MapPlacement { id: number; shape: number; rotation: number; x: number; y: number; plane: number; }
export interface MapFloor { id: number; overlay: boolean; rgb: number | null; textureId: number; secondaryRgb: number | null; textureScale: number; }
export interface MapRenderRegion {
    id: number; regionX: number; regionY: number;
    terrainStatus: "available" | "absent" | "failed";
    landscapeStatus: "available" | "absent" | "failed";
    planes: MapRenderPlane[]; objects: MapPlacement[]; floors: MapFloor[]; warnings: string[];
}
export interface ObjectRenderAppearance {
    id: number; shape: number; rotation: number; sizeX: number; sizeY: number; decorDisplacement: number;
    contourMode: number; contourParameter: number; parts: GetModelDto[]; warnings: string[];
}
export interface MapSelection { id: number; width: number; height: number; }
export interface MapLoadRequest extends MapSelection { keys: Record<string, number[]>; }
export interface MapVisibility { planes: readonly boolean[]; landscape: boolean; }
export interface ObjectAppearanceResult {
    id: number; shape: number; rotation: number; status: number;
    appearance: ObjectRenderAppearance | null;
    problem: { title?: string; detail?: string; status?: number } | null;
}
export interface MapLoadProgress {
    regionsSettled: number; regionsTotal: number;
    appearancesSettled: number; appearancesTotal: number;
    texturesSettled: number; texturesTotal: number;
    failures: number;
}
export const mapAppearanceKey = (placement: Pick<MapPlacement, "id" | "shape" | "rotation">) => `${placement.id}:${placement.shape}:${placement.rotation}`;

export function selectedMapIds(selection: MapSelection): number[] {
    const { id, width, height } = selection;
    if (![id, width, height].every(Number.isSafeInteger) || id < 0 || id > 65535 || width < 1 || width > 3 || height < 1 || height > 3) return [];
    const x = id >> 8, y = id & 255;
    if (x + width > 256 || y + height > 256) return [];
    return Array.from({ length: width * height }, (_, index) => id + Math.floor(index / height) * 256 + index % height);
}

export function parseMapKeys(text: string, ids: readonly number[]): Record<string, number[]> {
    const value: unknown = text.trim() ? JSON.parse(text) : {};
    if (!value || typeof value !== "object" || Array.isArray(value)) throw new Error("Enter an object keyed by selected region ID, with four signed integers per region.");
    const result: Record<string, number[]> = {};
    for (const [key, keys] of Object.entries(value)) {
        if (!ids.includes(Number(key)) || String(Number(key)) !== key) throw new Error(`Region ${key} is not in the selected rectangle.`);
        if (!Array.isArray(keys) || keys.length !== 4 || !keys.every(key => Number.isInteger(key) && key >= -2147483648 && key <= 2147483647))
            throw new Error(`Region ${key} requires four signed 32-bit integers.`);
        result[key] = keys;
    }
    return result;
}
