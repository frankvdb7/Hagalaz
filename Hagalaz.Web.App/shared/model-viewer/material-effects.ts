/** Source metadata retained independently of renderer or HTTP. */
export interface MaterialEffect {
    effectId?: number;
    effectParameter?: number;
    effectParameter2?: number;
    scrollU?: number;
    scrollV?: number;
}

/**
 * Effect 7 (GameClient Class49_Sub3): blend the unlit region cube over the lit base in gamma space.
 * The client negates the reflection ray with `glScalef(-1, -1, -1)`.
 */
export function clientReflection(amount: string, normal = "normal", viewPosition = "vViewPosition"): string {
    return `
    if (cacheEnvironmentEnabled) {
        vec3 cacheReflection = inverseTransformDirection(reflect(-normalize(${viewPosition}), ${normal}), viewMatrix);
        vec3 cacheCube = textureCube(cacheEnvironment, vec3(-cacheReflection.x, cacheReflection.y, -cacheReflection.z)).rgb;
        vec3 cacheBaseGamma = pow(max(outgoingLight, vec3(0.0)), vec3(1.0 / 2.2));
        vec3 cacheCubeGamma = pow(cacheCube, vec3(1.0 / 2.2));
        outgoingLight = pow(mix(cacheBaseGamma, cacheCubeGamma, ${amount}), vec3(2.2));
    }
`;
}

/** HD terrain water in client axes (+X east, +Y down, +Z north). */
export function clientHdWater(effect: 8 | 9, hasDepth: boolean) {
    return {
        vertexDeclarations: `
            ${hasDepth ? "attribute float waterDepth;" : ""}
            uniform vec2 cacheWaterScroll;
            varying vec3 cacheWaterEye;
            varying vec2 cacheWaterUv;
            varying float cacheWaterDepth;
            varying float cacheWaterFog;
            #ifdef USE_FOG
                uniform float fogNear;
                uniform float fogFar;
            #endif
        `,
        vertex: `
            vec3 waterEye = (modelMatrix * vec4(transformed, 1.0)).xyz - cameraPosition;
            cacheWaterEye = vec3(waterEye.x, -waterEye.y, waterEye.z);
            cacheWaterUv = vec2(uv.x, -uv.y) + cacheWaterScroll;
            cacheWaterDepth = ${hasDepth ? "waterDepth" : "0.0"};
            cacheWaterFog = 0.0;
            #ifdef USE_FOG
                cacheWaterFog = clamp((-mvPosition.z - fogNear) / (fogFar - fogNear), 0.0, 1.0);
            #endif
        `,
        fragmentDeclarations: `
            uniform highp sampler3D cacheWaterNoise;
            uniform samplerCube cacheEnvironment;
            uniform vec4 cacheWaterParams;
            uniform vec3 cacheWave;
            uniform vec3 cacheSunDirection;
            uniform vec3 cacheSunColor;
            varying vec3 cacheWaterEye;
            varying vec2 cacheWaterUv;
            varying float cacheWaterDepth;
            varying float cacheWaterFog;
        `,
        fragment: `
            vec4 waterNormal = texture(cacheWaterNoise, vec3(cacheWaterUv * cacheWaterParams.y, cacheWaterParams.x)).rbga;
            waterNormal.xyz = waterNormal.xyz * 2.0 - 1.0;
            vec3 waterEye = normalize(cacheWaterEye);
            vec3 waterReflection = reflect(waterEye, waterNormal.xyz);
            vec3 waterEnvironment = sRGBTransferOETF(vec4(textureCube(cacheEnvironment, waterReflection).rgb, 1.0)).rgb;
            float verticalSun = abs(cacheSunDirection.y);
            float sunExponent = ${effect === 8 ? "96.0" : "64.0"} + verticalSun * 928.0;
            vec4 sunColor = vec4(cacheSunColor ${effect === 9 ? "* (1.0 + (1.0 - verticalSun) * 2.0)" : ""}, 1.0);
            vec4 waterSpecular = sunColor * pow(clamp(-dot(cacheSunDirection, waterReflection), 0.0, 1.0), sunExponent);
            float shoreFactor = clamp(cacheWaterDepth / cacheWaterParams.z - cacheWaterParams.w * waterNormal.a, 0.0, 1.0);
            float fresnel = pow(1.0 - abs(dot(waterEye, waterNormal.xyz)), ${effect === 8 ? "2.0" : "1.0"});
            vec4 surfaceColor = vec4(waterEnvironment, fresnel * shoreFactor);
            ${effect === 9 ? `
                float waveFactor = pow(1.0 - shoreFactor, cacheWave.z) - 0.5;
                waveFactor = 1.0 - 4.0 * waveFactor * waveFactor;
                surfaceColor = mix(surfaceColor, cacheWave.x * waterNormal.aaaa + cacheWave.y, waveFactor);
            ` : ""}
            surfaceColor += waterSpecular * shoreFactor;
            if (surfaceColor.a <= 0.0) discard;
            #ifdef USE_FOG
                surfaceColor.rgb = mix(surfaceColor.rgb, fogColor, cacheWaterFog);
            #endif
            gl_FragColor = clamp(surfaceColor, 0.0, 1.0);
        `,
    };
}

export function isFixedWater(effect: number | undefined): effect is 2 | 4 | 5 {
    return effect === 2 || effect === 4 || effect === 5;
}

export function fixedWaterParameters(id: 2 | 4 | 5, sample: ReturnType<typeof sampleMaterialEffect>): readonly [number, number, number] {
    switch (id) {
        case 2: return [sample.animateWater ? sample.secondary : 0, Math.floor(sample.noise * 128), sample.distortion];
        case 4: return [sample.underwater, 0, 0];
        case 5: return [sample.lakeNoise, sample.lakeShift, sample.lakeScale];
    }
}

/** Class49_Sub4, Class49_Sub1 and Class49_Sub5 with their two texture units. */
export function clientFixedWater(effect: 2 | 4 | 5) {
    return {
        vertexDeclarations: `
            uniform vec4 cacheWaterParams;
            uniform vec4 cacheWaterFlags;
            uniform vec2 cacheWaterScroll;
            uniform vec3 cacheWaterAmbient;
            varying vec3 cacheWaterCoordinates;
            varying vec4 cacheWaterColor;
            ${effect === 2 ? `
                uniform highp sampler2D cacheTurbulence;
                float waterTurbulenceAt(int address) {
                    return texelFetch(cacheTurbulence, ivec2(address % 128, address / 128), 0).r;
                }
            ` : ""}
            vec3 waterVertexGamma(vec3 value) {
                return mix(1.055 * pow(max(value, vec3(0.0)), vec3(1.0 / 2.4)) - 0.055,
                    value * 12.92, lessThanEqual(value, vec3(0.0031308)));
            }
        `,
        vertex: `
            vec2 waterUv = vec2(uv.x, -uv.y) * ${effect === 4 ? "0.25" : "1.0"} + cacheWaterScroll;
            ${effect === 2 ? `
                vec2 world = (modelMatrix * vec4(transformed, 1.0)).xz;
                vec2 tile = floor(fract(vec2(world.x + world.y, world.y - world.x) / 8192.0) * 64.0);
                int offset = int(cacheWaterParams.y);
                vec2 turbulence = vec2(waterTurbulenceAt(offset + 2 * int(tile.x)),
                    waterTurbulenceAt(offset + 2 * int(tile.y) + 1));
                cacheWaterCoordinates = vec3(waterUv * 0.125, cacheWaterParams.x);
                waterUv += turbulence * cacheWaterParams.z;
            ` : effect === 4 ? `
                cacheWaterCoordinates = vec3(waterUv, cacheWaterParams.x);
            ` : `
                vec3 world = (modelMatrix * vec4(transformed, 1.0)).xyz;
                vec2 generated = vec2(mix(world.z, world.x, cacheWaterFlags.z), -world.y) * cacheWaterParams.z;
                generated.y += cacheWaterParams.y;
                cacheWaterCoordinates = vec3(generated, cacheWaterParams.x);
            `}
            vMapUv = vec2(waterUv.x, -waterUv.y);
            cacheWaterColor = vec4(1.0);
            #if defined(USE_COLOR) || defined(USE_COLOR_ALPHA)
                cacheWaterColor.rgb = waterVertexGamma(vColor.rgb);
            #endif
            #ifdef USE_COLOR_ALPHA
                cacheWaterColor.a = vColor.a;
            #endif
            ${effect === 2 ? "if (cacheWaterFlags.y > 0.5) cacheWaterColor.rgb *= cacheWaterAmbient;" : ""}
            cacheWaterColor = clamp(cacheWaterColor, 0.0, 1.0);
        `,
        fragmentDeclarations: `
            uniform highp sampler3D cacheWaterNoise;
            uniform vec4 cacheWaterFlags;
            uniform float cacheWaterFogWidth;
            varying vec3 cacheWaterCoordinates;
            varying vec4 cacheWaterColor;
        `,
        fragment: `
            vec4 waterColor = cacheWaterColor;
            ${effect === 4 ? `
                if (cacheWaterFlags.w > 0.5) {
                    vec2 noise = texture(cacheWaterNoise, cacheWaterCoordinates).rg;
                    waterColor = vec4(clamp(2.0 * (waterColor.rgb + noise.r), 0.0, 1.0), waterColor.a * noise.g);
                }
            ` : `
                // TextureLoader's GL combine column is MODULATE for all measured water materials.
                vec4 base = sRGBTransferOETF(sampledDiffuseColor);
                waterColor *= base;
                if (cacheWaterFlags.w > 0.5 ${effect === 2 ? "&& cacheWaterFlags.x > 0.5" : ""}) {
                    vec2 noise = texture(cacheWaterNoise, cacheWaterCoordinates).rg;
                    ${effect === 2
                        ? "waterColor = vec4(clamp(waterColor.rgb + noise.r, 0.0, 1.0), 1.0);"
                        : "waterColor *= vec4(vec3(noise.r), noise.g);"}
                }
            `}
            #ifdef USE_FOG
                float waterFogNear = fogNear;
                float waterFogFar = fogFar;
                ${effect === 4 ? `
                    // Class36_Sub4: two alpha texels (0, 1), linear filter, clamp to edge.
                    float rampCoordinate = 0.25 - (vFogDepth + fogNear) * 0.5 / cacheWaterFogWidth;
                    float rampAlpha = clamp(rampCoordinate * 2.0 - 0.5, 0.0, 1.0);
                    waterColor.a = min(1.0, waterColor.a + rampAlpha);
                    waterFogFar -= cacheWaterFogWidth;
                    waterFogNear = max(200.0, waterFogFar - 0.5 * cacheWaterFogWidth);
                ` : ""}
                float waterFog = clamp((vFogDepth - waterFogNear) / max(waterFogFar - waterFogNear, 1.0), 0.0, 1.0);
                waterColor.rgb = mix(waterColor.rgb, fogColor, waterFog);
            #endif
            if (waterColor.a <= 0.0) discard;
            gl_FragColor = waterColor;
        `,
    };
}

export function describeMaterialEffect(effect: MaterialEffect): string | null {
    const id = effect.effectId ?? 0;
    switch (id) {
        case 0: return null;
        case 1: return `Environment map #1, lobe ${effect.effectParameter ?? 0}: client specular highlight.`;
        case 2: return "Ordinary water #2 uses the client turbulence and animated noise stages.";
        case 3: return "Effect #3 is unresolved; base-material fallback.";
        case 4: return "Underwater #4 uses the client noise and eye-depth texture stages.";
        case 5: return "Lake water #5 uses the client generated coordinates and animated noise.";
        case 6: return "Light absorb #6: unlit material, matching the client lighting-disable state.";
        case 7: return "Reflection #7 uses the region environment map. No reflection without one.";
        case 8: return "HD water #8 uses the client shoreline and region reflection. Base material without a region cube.";
        case 9: return "HD water wave #9 uses the client wave and region reflection. Base material without a region cube.";
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
