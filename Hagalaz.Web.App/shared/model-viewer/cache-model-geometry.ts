import { clampLight, lightModel, litFaceColor } from "./model-lighting";
import type { ModelGeometry } from "./model-scene";
import type {ModelParticle} from "./model-particles";

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
    particles?: readonly ModelParticle[];
    coordinateScale?: number;
}

/** Converts the cache's downward Y axis and reverses winding exactly once. */
export function prepareCacheModel(model: CacheModelGeometry, materials?: ReadonlyMap<number, { colorBlend?: number; brightness?: number; effectId?: number }>): ModelGeometry {
    const coordinate = (index: number) => model.scale ? Math.floor(model.positions[index] * model.scale[index % 3]) : model.positions[index];
    if (model.faceColors) {
        const preparedLighting = model.ambient !== undefined || model.faceShadingTypes !== undefined;
        const shades = preparedLighting ? lightModel({...model, positions: model.positions.map((_, index) => Math.trunc(coordinate(index) / (model.coordinateScale ?? 1)))}) : [];
        const positions: number[] = [];
        const colors: number[] = [];
        const indices: number[] = [];
        const opacities: number[] = [];
        const uvs: number[] = [];
        const textureIds: number[] = [];
        for (let face = 0; face < model.indices.length / 3; face++) {
            const decodedUvs = model.faceTextureUvs?.slice(face * 6, face * 6 + 6);
            const textured = (model.faceTextureIds?.[face] ?? -1) >= 0 && decodedUvs?.length === 6;
            textureIds.push(textured ? model.faceTextureIds![face] : -1);
            const material = textured ? materials?.get(model.faceTextureIds![face]) : undefined;
            const rgb = textured && !material ? [1, 1, 1] : packedHslColor(model.faceColors[face], material);
            const alpha = model.faceAlpha?.[face] ?? 0;
            // Client alpha -1 hides helper faces; -2 uses constant shading rather than transparency.
            const type = alpha === 255 ? 2 : alpha === 254 ? 3 : model.faceShadingTypes?.[face] ?? 0;
            const opacity = alpha === 255 || type === 2 || (preparedLighting && textured && type === 3) ? 0 : alpha === 254 ? 1 : (255 - alpha) / 255;
            for (const corner of [0, 2, 1]) {
                const vertex = model.indices[face * 3 + corner] * 3;
                positions.push(coordinate(vertex), -coordinate(vertex + 1), coordinate(vertex + 2));
                if (preparedLighting) {
                    const intensity = material?.effectId === 6 ? 127 : textured ? clampLight(shades[face * 3 + corner] >> 1) : shades[face * 3 + corner];
                    let channels = type === 3 ? [0, 0, 0] : litFaceColor(model.faceColors[face], intensity, textured, model.ambient);
                    if (textured && material) channels = channels.map(channel => {
                        const blend = material.colorBlend ?? 0;
                        const mixed = (channel * (256 - blend) + 2 * intensity * blend) >> 8;
                        return Math.min(65535, mixed * (256 + (material.brightness ?? 0))) >> 8;
                    });
                    colors.push(...channels.map(channel => { const value = channel / 255; return value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4; }));
                } else colors.push(...rgb);
                indices.push(indices.length);
                opacities.push(opacity);
                uvs.push(...(textured ? decodedUvs!.slice(corner * 2, corner * 2 + 2) : [0, 0]));
            }
        }
        return { preparedLighting, sourceId: model.id, facePriorities: model.facePriorities ?? undefined, positions, indices, colors, opacities, uvs, textureIds, particles: model.particles, particleScale: model.coordinateScale ?? 1 };
    }
    return {
        sourceId: model.id, facePriorities: model.facePriorities ?? undefined,
        particles: model.particles,
        particleScale: model.coordinateScale ?? 1,
        positions: model.positions.map((_, index) => index % 3 === 1 ? -coordinate(index) : coordinate(index)),
        indices: model.indices.flatMap((_, index, source) => index % 3 === 0 ? [source[index], source[index + 2], source[index + 1]] : []),
    };
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
        return srgb <= 0.04045 ? srgb / 12.92 : ((srgb + 0.055) / 1.055) ** 2.4;
    });
}
