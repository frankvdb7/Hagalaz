import { clampLight, lightModel, litFaceColor, pointLightRgb, type ModelLight } from "./model-lighting";
import type { ModelGeometry } from "./model-scene";
import type {ModelEffector, ModelParticle, PosedEffector} from "./model-particles";
import type { ModelBillboard, RenderBillboard } from "./model-billboards";

export const modelFaceWinding = [0, 2, 1] as const;
const linearChannels = Array.from({ length: 256 }, (_, channel) => toLinear(channel / 255));

function toLinear(srgb: number): number {
    return srgb <= 0.04045 ? srgb / 12.92 : ((srgb + 0.055) / 1.055) ** 2.4;
}

export interface CacheModelGeometry {
    id?: number;
    faceShadingTypes?: readonly number[] | null;
    ambient?: number;
    contrast?: number;
    positions: readonly number[];
    scale?: readonly number[] | null;
    indices: readonly number[];
    faceColors?: readonly number[];
    faceAlpha?: readonly number[];
    facePriorities?: readonly number[] | null;
    faceTextureIds?: readonly number[] | null;
    faceTextureUvs?: readonly number[] | null;
    firstTexelFaces?: readonly number[] | null;
    particles?: readonly ModelParticle[];
    effectors?: readonly ModelEffector[] | null;
    billboards?: readonly ModelBillboard[] | null;
    coordinateScale?: number;
}

export type FaceMaterial = { colorBlend?: number; brightness?: number; effectId?: number };

type Coordinate = (index: number) => number;

/** Output buffers of a model whose faces are expanded to independent corners. */
interface FaceBuffers {
    positions: number[];
    colors: number[];
    indices: number[];
    opacities: number[];
    uvs: number[];
    textureIds: number[];
    firstTexel: number[];
}

interface FaceSource {
    model: CacheModelGeometry;
    faceColors: readonly number[];
    coordinate: Coordinate;
    materials?: ReadonlyMap<number, FaceMaterial>;
    preparedLighting: boolean;
    shades: readonly number[];
    hiddenFaces: ReadonlySet<number>;
    firstTexelFaces: ReadonlySet<number>;
}

interface FaceTexture {
    textureId: number;
    uvs?: readonly number[];
    material?: FaceMaterial;
}

function faceTexture(model: CacheModelGeometry, face: number, materials: ReadonlyMap<number, FaceMaterial> | undefined): FaceTexture {
    const uvs = model.faceTextureUvs?.slice(face * 6, face * 6 + 6);
    const textureId = model.faceTextureIds?.[face] ?? -1;
    if (textureId < 0 || uvs?.length !== 6) return { textureId: -1 };
    return { textureId, uvs, material: materials?.get(textureId) };
}

/** Client alpha -1 (255) hides helper faces; -2 (254) uses constant shading rather than transparency. */
function faceShadingType(model: CacheModelGeometry, face: number, alpha: number): number {
    if (alpha === 255) return 2;
    if (alpha === 254) return 3;
    return model.faceShadingTypes?.[face] ?? 0;
}

function unlitFaceColor(packed: number, texture: FaceTexture): number[] {
    return texture.uvs && !texture.material ? [1, 1, 1] : packedHslColor(packed, texture.material);
}

function appendFace(source: FaceSource, buffers: FaceBuffers, face: number) {
    const { model, coordinate, preparedLighting } = source;
    const texture = faceTexture(model, face, source.materials);
    const textured = texture.uvs !== undefined;
    const alpha = model.faceAlpha?.[face] ?? 0;
    const type = faceShadingType(model, face, alpha);
    const opacity = faceOpacity(source.hiddenFaces, face, alpha, type, preparedLighting && textured);
    const rgb = preparedLighting ? [] : unlitFaceColor(source.faceColors[face], texture);
    const texel = source.firstTexelFaces.has(face) ? 1 : 0;
    buffers.textureIds.push(texture.textureId);
    for (const corner of modelFaceWinding) {
        const vertex = model.indices[face * 3 + corner] * 3;
        buffers.positions.push(coordinate(vertex), -coordinate(vertex + 1), coordinate(vertex + 2));
        if (preparedLighting) {
            appendLitCorner(buffers.colors, source.faceColors[face], source.shades[face * 3 + corner], textured, texture.material, type, model.ambient);
        } else buffers.colors.push(...rgb);
        buffers.indices.push(buffers.indices.length);
        buffers.opacities.push(opacity);
        buffers.firstTexel.push(texel);
        buffers.uvs.push(...(texture.uvs?.slice(corner * 2, corner * 2 + 2) ?? [0, 0]));
    }
}

function prepareColoredModel(model: CacheModelGeometry, faceColors: readonly number[], coordinate: Coordinate,
    materials: ReadonlyMap<number, FaceMaterial> | undefined, light: ModelLight | undefined) {
    const preparedLighting = model.ambient !== undefined || model.faceShadingTypes !== undefined;
    const lightingPositions = () => model.positions.map((_, index) => Math.trunc(coordinate(index) / (model.coordinateScale ?? 1)));
    const shades = preparedLighting ? lightModel({ ...model, positions: lightingPositions() }, light) : [];
    const firstTexelFaces = new Set(model.firstTexelFaces ?? []);
    const { billboards, hiddenFaces } = prepareBillboards(model, faceColors);
    const source: FaceSource = { model, faceColors, coordinate, materials, preparedLighting, shades, hiddenFaces, firstTexelFaces };
    const buffers: FaceBuffers = { positions: [], colors: [], indices: [], opacities: [], uvs: [], textureIds: [], firstTexel: [] };
    for (let face = 0; face < model.indices.length / 3; face++) appendFace(source, buffers, face);
    const { firstTexel, ...faces } = buffers;
    return { ...faces, preparedLighting, billboards, firstTexel: firstTexelFaces.size ? firstTexel : undefined };
}

/** Converts the cache's downward Y axis and reverses winding exactly once. */
export function prepareCacheModel(model: CacheModelGeometry, materials?: ReadonlyMap<number, FaceMaterial>, light?: ModelLight): ModelGeometry {
    const scale = model.scale;
    const coordinate: Coordinate = scale ? index => Math.floor(model.positions[index] * scale[index % 3]) : index => model.positions[index];
    const shared = {
        sourceId: model.id,
        facePriorities: model.facePriorities ?? undefined,
        particles: model.particles,
        effectors: prepareEffectors(model.effectors, coordinate),
        particleScale: model.coordinateScale ?? 1,
    };
    if (model.faceColors) return { ...shared, ...prepareColoredModel(model, model.faceColors, coordinate, materials, light) };
    return {
        ...shared,
        positions: model.positions.map((_, index) => index % 3 === 1 ? -coordinate(index) : coordinate(index)),
        indices: model.indices.flatMap((_, index, source) => index % 3 === 0 ? [source[index], source[index + 2], source[index + 1]] : []),
    };
}

function prepareEffectors(effectors: CacheModelGeometry["effectors"], coordinate: (index: number) => number): PosedEffector[] | undefined {
    return effectors?.map(({ effectorId, vertex, definition }) => ({
        effectorId, definition, position: [coordinate(vertex * 3), -coordinate(vertex * 3 + 1), coordinate(vertex * 3 + 2)],
    }));
}

function appendLitCorner(colors: number[], packed: number, shade: number, textured: boolean, material: FaceMaterial | undefined, type: number, ambient: number | undefined): void {
    const intensity = material?.effectId === 6 ? 127 : textured ? clampLight(shade >> 1) : shade;
    const channels = type === 3 ? [0, 0, 0] : litFaceColor(packed, intensity, textured, ambient);
    if (material) appendMaterialChannels(colors, channels, material, intensity);
    else colors.push(linearChannels[channels[0]], linearChannels[channels[1]], linearChannels[channels[2]]);
}

function appendMaterialChannels(colors: number[], channels: readonly number[], material: FaceMaterial, intensity: number): void {
    const blend = material.colorBlend ?? 0;
    const brightness = material.brightness ?? 0;
    for (const channel of channels) {
        const mixed = (channel * (256 - blend) + 2 * intensity * blend) >> 8;
        const value = Math.min(65535, mixed * (256 + brightness)) >> 8;
        colors.push(linearChannels[value] ?? toLinear(value / 255));
    }
}

function faceOpacity(hiddenFaces: ReadonlySet<number>, face: number, alpha: number, type: number, litTexture: boolean | undefined): number {
    if (hiddenFaces.has(face) || alpha === 255 || type === 2 || (litTexture && type === 3)) return 0;
    return alpha === 254 ? 1 : (255 - alpha) / 255;
}

const baseBillboardPose = { x: 0, y: 0, angle: 0, scaleX: 128, scaleY: 128 };

function prepareBillboards(model: CacheModelGeometry, colors: readonly number[]) {
    const hiddenFaces = new Set<number>();
    const scale = model.coordinateScale ?? 1;
    const billboards = model.billboards?.flatMap(({ face, definition, pose = baseBillboardPose, depthOffset }): RenderBillboard[] => {
        if (!definition) return [];
        if (definition.hideFace) hiddenFaces.add(face);
        return [{
            face, textureId: definition.textureId, blendMode: definition.blendMode,
            halfWidth: ((definition.halfWidth * pose.scaleX) >> 7) * scale,
            halfHeight: ((definition.halfHeight * pose.scaleY) >> 7) * scale,
            x: pose.x * scale, y: pose.y * scale, angle: pose.angle,
            depthOffset: depthOffset * scale, rgb: pointLightRgb(colors[face]),
            opacity: (255 - (model.faceAlpha?.[face] ?? 0)) / 255,
        }];
    });
    return { billboards, hiddenFaces };
}

/** Decodes the cache's 6-bit hue, 3-bit saturation and 7-bit lightness. */
function packedHslColor(value: number, material?: { colorBlend?: number; brightness?: number }): number[] {
    const hue = ((value >> 10) & 63) / 64 + 1 / 128;
    const saturation = ((value >> 7) & 7) / 8 + 1 / 16;
    const lightness = (value & 127) / 128;
    const q = lightness < 0.5 ? lightness * (1 + saturation) : lightness + saturation - lightness * saturation;
    const p = 2 * lightness - q;
    return [hue + 1 / 3, hue, hue - 1 / 3].map((channel) => {
        const t = (channel + 1) % 1;
        let srgb = t < 1 / 6 ? p + (q - p) * 6 * t : t < 0.5 ? q : t < 2 / 3 ? p + (q - p) * (2 / 3 - t) * 6 : p;
        if (material) {
            // Neutral shade 127; GameClient Model_Sub3.method4552 mixes in sRGB before brightness.
            const blend = (material.colorBlend ?? 0) / 256;
            srgb = Math.min(1, (srgb * (1 - blend) + (254 / 255) * blend) * (1 + (material.brightness ?? 0) / 256));
        }
        return toLinear(srgb);
    });
}
