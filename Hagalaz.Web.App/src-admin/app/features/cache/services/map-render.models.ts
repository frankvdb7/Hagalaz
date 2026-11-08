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
export interface MapAtmosphere {
    lightRgb: number; intensity: number; positiveResponse: number; negativeResponse: number;
    directionX: number; directionY: number; directionZ: number;
    /** Scene clear and fog colour. */
    skyRgb: number;
    /** Raw revision-742 fog input, not a density. */
    fogValue: number;
}
export interface MapRenderRegion {
    id: number; regionX: number; regionY: number;
    terrainStatus: "available" | "absent" | "failed";
    landscapeStatus: "available" | "absent" | "failed";
    planes: MapRenderPlane[]; objects: MapPlacement[]; floors: MapFloor[]; warnings: string[];
    atmosphere?: MapAtmosphere | null;
}
export interface ObjectRenderAppearance {
    name?: string | null;
    id: number; shape: number; rotation: number; sizeX: number; sizeY: number; decorDisplacement: number;
    contourMode: number; contourParameter: number; parts: GetModelDto[]; warnings: string[];
}
export interface MapSelection { id: number; width: number; height: number; }
export interface MapLoadRequest extends MapSelection { keys: Record<string, number[]>; }
export interface MapVisibility { planes: readonly boolean[]; landscape: boolean; previewPlacements?: readonly MapPlacement[]; }
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
// Revision-742 projection planes for the standard 104-tile scene, in scene units.
const clientNearPlane = 200;
const clientFarPlane = Math.floor(34.46 * 104) << 2;

/** Linear eye-depth fog range used by the client's OpenGL toolkit. */
export function mapFogRange(fogValue: number): { start: number; end: number } {
    return { start: Math.max(clientNearPlane, clientFarPlane - ((fogValue + 256) << 2)), end: clientFarPlane };
}
export const mapAppearanceKey = (placement: Pick<MapPlacement, "id" | "shape" | "rotation">) => `${placement.id}:${placement.shape}:${placement.rotation}`;

export function selectedMapIds(selection: MapSelection): number[] {
    const { id, width, height } = selection;
    if (![id, width, height].every(Number.isSafeInteger) || id < 0 || id > 65535 || width < 1 || width > 3 || height < 1 || height > 3) return [];
    const x = id >> 8, y = id & 255;
    if (x + width > 256 || y + height > 256) return [];
    return Array.from({ length: width * height }, (_, index) => id + Math.floor(index / height) * 256 + index % height);
}
