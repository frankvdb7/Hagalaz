import * as THREE from "three";

export interface MapLightField {
    width: number;
    height: number;
    tiles: Float32Array;
    lights: readonly { x: number; y: number; z: number; radius: number; rgb: number }[];
}

const lightDeclarations = /* glsl */ `
    uniform highp sampler2D mapLightTiles;
    uniform highp sampler2D mapLightRecords;
    uniform int mapLightHeight;
    varying vec3 mapLightPosition;
    flat varying int mapLightPlane;

    vec3 mapLocalLight() {
        if (mapLightHeight == 0 || mapLightPlane >= 4) return vec3(0.0);
        // Take derivatives before the tile lookup can send adjacent fragments down different paths.
        vec3 dx = dFdx(mapLightPosition);
        vec3 dy = dFdy(mapLightPosition);
        ivec2 tile = ivec2(floor(mapLightPosition.xz / 512.0));
        if (any(lessThan(tile, ivec2(0))) || tile.y >= mapLightHeight ||
            tile.x >= textureSize(mapLightTiles, 0).x) return vec3(0.0);
        tile.y += mapLightPlane * mapLightHeight;
        ivec4 ids = ivec4(texelFetch(mapLightTiles, tile, 0));
        if (ids.x == 0) return vec3(0.0);
        vec3 normal = normalize(cross(dx, dy));
        vec3 light = vec3(0.0);
        for (int slot = 0; slot < 4; slot++) {
            if (ids[slot] == 0) break;
            int index = (ids[slot] - 1) * 2;
            ivec2 address = ivec2(index % 256, index / 256);
            vec4 source = texelFetch(mapLightRecords, address, 0);
            vec3 delta = (source.xyz - mapLightPosition) / source.w;
            float distanceSq = dot(delta, delta);
            if (distanceSq >= 1.0) continue;
            float facing = max(0.0, dot(normal, delta) * inversesqrt(max(distanceSq, 0.000001)));
            float strength = clamp(2.0 * facing * (1.0 - distanceSq), 0.0, 1.0);
            light += texelFetch(mapLightRecords, address + ivec2(1, 0), 0).rgb * strength;
        }
        return light;
    }
`;

/** Shared uniforms keep light updates independent of geometry and material changes. */
export class MapLightMaterial {
    private field: MapLightField | null = null;
    private readonly height = { value: 0 };
    private readonly tiles = { value: null as THREE.DataTexture | null };
    private readonly records = { value: null as THREE.DataTexture | null };

    set(field: MapLightField | null, enabled: boolean): void {
        if (this.field !== field) {
            this.dispose();
            this.field = field;
            if (field) {
                const rows = Math.ceil(field.lights.length / 128);
                const records = new Float32Array(rows * 256 * 4);
                field.lights.forEach(({ x, y, z, radius, rgb }, index) => {
                    records.set([x, y, z, radius, (rgb >> 16 & 255) / 255, (rgb >> 8 & 255) / 255, (rgb & 255) / 255, 0], index * 8);
                });
                this.tiles.value = lightTexture(field.tiles, field.width, field.height * 4);
                this.records.value = lightTexture(records, 256, rows);
            }
        }
        this.height.value = enabled ? field?.height ?? 0 : 0;
    }

    apply(material: THREE.Material): void {
        const compile = material.onBeforeCompile;
        const programKey = material.customProgramCacheKey();
        material.customProgramCacheKey = () => `${programKey}:map-point-lights`;
        material.onBeforeCompile = (shader, renderer) => {
            compile.call(material, shader, renderer);
            shader.uniforms["mapLightHeight"] = this.height;
            shader.uniforms["mapLightTiles"] = this.tiles;
            shader.uniforms["mapLightRecords"] = this.records;
            shader.vertexShader = `attribute float mapPlane;\nvarying vec3 mapLightPosition;\nflat varying int mapLightPlane;\n${shader.vertexShader}`
                .replace("#include <begin_vertex>", "#include <begin_vertex>\nmapLightPosition = transformed; mapLightPlane = int(mapPlane);");
            shader.fragmentShader = `${lightDeclarations}\n${shader.fragmentShader}`
                // The hardware terrain pass adds light multiplied by the existing framebuffer color.
                .replace("#include <colorspace_fragment>", "#include <colorspace_fragment>\ngl_FragColor.rgb *= 1.0 + mapLocalLight();");
        };
    }

    dispose(): void {
        this.tiles.value?.dispose();
        this.records.value?.dispose();
        this.tiles.value = this.records.value = null;
        this.height.value = 0;
        this.field = null;
    }
}

function lightTexture(data: Float32Array, width: number, height: number): THREE.DataTexture {
    const texture = new THREE.DataTexture(data, width, height, THREE.RGBAFormat, THREE.FloatType);
    texture.needsUpdate = true;
    return texture;
}
