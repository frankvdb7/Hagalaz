/** Source metadata retained independently of renderer or HTTP. */
export interface MaterialEffect {
    effectId?: number;
    effectParameter?: number;
    effectParameter2?: number;
    scrollU?: number;
    scrollV?: number;
}

export function describeMaterialEffect(effect: MaterialEffect): string | null {
    const id = effect.effectId ?? 0;
    switch (id) {
        case 0: return null;
        case 1: return `Environment map #1, lobe ${effect.effectParameter ?? 0}: approximation using an inspection sky.`;
        case 2: return "Ordinary water #2: procedural-noise approximation; client noise and scene lighting unavailable.";
        case 3: return "Effect #3 is unresolved; base-material fallback.";
        case 4: return "Underwater #4: animated-noise approximation; scene depth and fog unavailable.";
        case 5: return "Lake water #5: generated-coordinate approximation; client noise and scene lighting unavailable.";
        case 6: return "Light absorb #6: unlit material, matching the client lighting-disable state.";
        case 7: return "Reflection #7: approximation using an inspection sky; client environment unavailable.";
        case 8: return "HD water #8: procedural-normal approximation; shoreline and client environment unavailable.";
        case 9: return "HD water wave #9: procedural-normal approximation; shoreline foam unavailable.";
        default: return `Effect #${id} is unsupported; base-material fallback.`;
    }
}

/** Client time wraps at 128 seconds for scrolling, 40 seconds for HD noise. */
export function sampleMaterialEffect(effect: MaterialEffect, seconds: number) {
    const p = effect.effectParameter ?? 0;
    const packed = effect.effectParameter2 ?? 0;
    const milliseconds = Math.floor(seconds * 1000);
    return {
        u: (((milliseconds % 128000) / 1000) * (effect.scrollU ?? 0) / 64) % 1,
        v: (((milliseconds % 128000) / 1000) * (effect.scrollV ?? 0) / 64) % 1,
        noise: (milliseconds % 5000) / 5000,
        secondary: (milliseconds % 4000) / 4000,
        underwater: (p & 1) === 1 ? (milliseconds % 4000) / 4000 : 0,
        distortion: (p & 3) === 2 ? 0.05 : (p & 3) === 3 ? 0.1 : 0.025,
        animateWater: (packed & 1) === 1 && (p & 128) === 0,
        lakeShift: (milliseconds * -0.0005 * ((p & 3) + 1)) % 1,
        lakeNoise: (milliseconds * 0.0005 * (((p >> 3) & 3) + 1)) % 1,
        lakeScale: (p & 64) === 0 ? 1 / 2048 : 1 / 1024,
        lakeXAxis: (p & 128) !== 0,
        hdPhase: ((milliseconds * (1 << (p & 3))) % 40000) / 40000,
        hdScale: (1 << ((p >> 3) & 7)) / 32,
        breakDepth: packed & 0xffff,
        breakOffset: ((packed >>> 16) & 3) / 8,
        waveIntensity: [((packed >>> 23) & 15) / 16, ((packed >>> 19) & 15) / 16],
        waveExponent: (packed >>> 27) & 15,
    };
}
