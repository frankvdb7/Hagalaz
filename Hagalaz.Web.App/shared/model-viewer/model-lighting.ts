/** Isolated software-client lighting. Scene atmosphere and palette jitter are fixed for inspection. */
export interface LightingModel {
    positions: readonly number[];
    indices: readonly number[];
    faceShadingTypes?: readonly number[] | null;
    ambient?: number;
    contrast?: number;
}

// Atmosphere defaults, GraphicsToolkit_Sub3.m/IA and Model_Sub3.method4549.
const direction = [-50, -60, -50].map(value => Math.trunc(value * 65535 / Math.sqrt(8600)));
const baseLight = Math.trunc(Math.fround(1.1523438) * 65535) >> 8;
const positive = Math.trunc(Math.fround(0.69921875) * 65535);
const negative = Math.trunc(Math.fround(1.2) * 65535);

/** Source face normals and smooth sums are calculated before expanding face corners. */
export function lightModel(model: LightingModel): number[] {
    const sums = Array.from({length: model.positions.length / 3}, () => [0, 0, 0, 0]);
    const normals: number[][] = [];
    for (let face = 0; face < model.indices.length / 3; face++) {
        const [a, b, c] = model.indices.slice(face * 3, face * 3 + 3).map(vertex => vertex * 3);
        const ab = [0, 1, 2].map(axis => model.positions[b + axis] - model.positions[a + axis]);
        const ac = [0, 1, 2].map(axis => model.positions[c + axis] - model.positions[a + axis]);
        let x = (ab[1] * ac[2] - ac[1] * ab[2]) | 0;
        let y = (ab[2] * ac[0] - ac[2] * ab[0]) | 0;
        let z = (ab[0] * ac[1] - ac[0] * ab[1]) | 0;
        while (Math.max(Math.abs(x), Math.abs(y), Math.abs(z)) > 8192) { x >>= 1; y >>= 1; z >>= 1; }
        const length = Math.max(1, Math.trunc(Math.sqrt(x * x + y * y + z * z)));
        const normal = [x, y, z].map(value => Math.trunc(value * 256 / length));
        normals.push(normal);
        if ((model.faceShadingTypes?.[face] ?? 0) === 0) {
            for (const vertex of [a / 3, b / 3, c / 3]) {
                for (let axis = 0; axis < 3; axis++) sums[vertex][axis] += normal[axis];
                sums[vertex][3]++;
            }
        }
    }
    const contrast = model.contrast ?? 850;
    const responses = contrast === 0 ? [0, 0] : [positive, negative].map(value => Math.trunc(value * 768 / contrast));
    return model.indices.map((vertex, corner) => {
        const face = Math.trunc(corner / 3);
        const flat = model.faceShadingTypes?.[face] === 1;
        const normal = flat ? normals[face] : sums[vertex];
        const count = flat ? 1 : Math.max(1, normal[3]);
        const dot = Math.trunc((Math.imul(direction[0], normal[0]) + Math.imul(direction[1], normal[1]) + Math.imul(direction[2], normal[2]) | 0) / count) >> 16;
        // This client threshold is 256, not the sign of the dot product.
        return (baseLight >> 1) + (Math.imul(responses[dot > 256 ? 0 : 1], dot) >> 17);
    });
}

export function clampLight(value: number): number { return Math.max(2, Math.min(126, value)); }

/** Model_Sub3.method4549 + Class356.method4187 + the fixed-gamma client palettes. */
export function litFaceColor(packed: number, intensity: number, textured: boolean, ambient = 64): number[] {
    if (textured) return paletteColor((packed & 0xff80) | clampLight((packed & 127) * intensity >> 7), false);
    const prepared = (packed & ~127) | ((packed & 127) * ambient >> 7);
    const hsv = softwareHsv(prepared);
    return paletteColor((hsv & 0xff80) | clampLight((hsv & 127) * intensity >> 7), true);
}

/** Class491.method6074 + Class230.method2338; unlit base tint for Class_xa_Sub3 floors. */
export function floorPaletteColor(rgb: number): number[] {
    const hsl = floorHsl(rgb);
    return floorHslPaletteColor(Math.trunc(hsl[0] * 256 / hsl[3]), hsl[1], hsl[2]);
}

/** Weighted hue, saturation, lightness and hue multiplier, as stored by Class491. */
export function floorHsl(rgb: number): readonly number[] {
    const [r, g, b] = [(rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255].map(channel => channel / 256);
    const min = Math.min(r, g, b), max = Math.max(r, g, b);
    const lightness = (max + min) / 2;
    let hue = 0, saturation = 0;
    if (max !== min) {
        saturation = lightness < 0.5 ? (max - min) / (max + min) : (max - min) / (2 - max - min);
        hue = (max === r ? (g - b) / (max - min) : max === g ? 2 + (b - r) / (max - min) : 4 + (r - g) / (max - min)) / 6;
    }
    const multiplier = Math.max(1, Math.trunc(saturation * (lightness > 0.5 ? 1 - lightness : lightness) * 512));
    const weightedHue = Math.trunc(hue * multiplier);
    return [weightedHue, Math.min(255, Math.trunc(saturation * 256)), Math.min(255, Math.trunc(lightness * 256)), multiplier];
}

/** Class230 packing followed by the software floor palette, after neighbourhood averaging. */
export function floorHslPaletteColor(h: number, saturation: number, l: number): number[] {
    let s = saturation;
    s >>= l > 243 ? 4 : l > 217 ? 3 : l > 192 ? 2 : l > 179 ? 1 : 0;
    const packed = ((h & 255) >> 2 << 10) + (s >> 5 << 7) + (l >> 1);
    return paletteColor(softwareHsv(packed), true);
}

/** Class356.method4187, shared by software model and terrain palettes. */
function softwareHsv(packed: number): number {
    const hue = packed >> 10 & 63;
    let saturation = packed >> 3 & 112;
    const lightness = packed & 127;
    saturation = lightness <= 64 ? saturation * lightness >> 7 : saturation * (127 - lightness) >> 7;
    const value = saturation + lightness;
    const hsvSaturation = value !== 0 ? Math.trunc((saturation << 8) / value) : saturation << 1;
    return (hue << 10 | (hsvSaturation >> 4) << 7 | value) & 65535;
}

function paletteColor(packed: number, hsv: boolean): number[] {
    const hue = (packed >> 10 & 63) / 64 + 1 / 128;
    const saturation = (packed >> 7 & 7) / 8 + 1 / 16;
    const value = (packed & 127) / 128;
    let channels: number[];
    if (hsv) {
        const sector = hue * 6, index = Math.trunc(sector), fraction = sector - index;
        const p = value * (1 - saturation), q = value * (1 - saturation * fraction), t = value * (1 - (1 - fraction) * saturation);
        channels = [[value,t,p],[q,value,p],[p,value,t],[p,q,value],[t,p,value],[value,p,q]][index % 6];
    } else {
        const q = value < 0.5 ? value * (1 + saturation) : value + saturation - value * saturation;
        const p = 2 * value - q;
        channels = [hue + 1 / 3, hue, hue - 1 / 3].map(channel => {
            const t = (channel + 1) % 1;
            return t < 1 / 6 ? p + (q - p) * 6 * t : t < 0.5 ? q : t < 2 / 3 ? p + (q - p) * (2 / 3 - t) * 6 : p;
        });
    }
    // The client randomly jitters gamma around 0.7. Inspection uses exactly 0.7.
    return channels.map(channel => Math.trunc(Math.pow(channel, 0.7) * 256) & 255);
}
