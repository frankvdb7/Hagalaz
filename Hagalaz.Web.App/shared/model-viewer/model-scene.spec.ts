import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import * as THREE from "three";
import { ModelScene, type ModelTexture } from "./model-scene";

const gpu = vi.hoisted(() => ({
    setSize: vi.fn(), setAnimationLoop: vi.fn(), dispose: vi.fn(), remove: vi.fn(),
    disconnect: vi.fn(), controlsDispose: vi.fn(), render: vi.fn(),
    gizmoRender: vi.fn(), gizmoDispose: vi.fn(),
}));

vi.mock("three", async importOriginal => {
    const actual = await importOriginal<typeof import("three")>();
    return { ...actual, WebGLRenderer: class {
        autoClear = true;
        domElement = { setAttribute: vi.fn(), remove: gpu.remove };
        setPixelRatio = vi.fn(); setSize = gpu.setSize; setAnimationLoop = gpu.setAnimationLoop;
        render = gpu.render; dispose = gpu.dispose;
    } };
});
vi.mock("three/addons/controls/OrbitControls.js", () => ({ OrbitControls: class {
    target = new THREE.Vector3(); update = vi.fn(); dispose = gpu.controlsDispose;
} }));
vi.mock("./orientation-gizmo", () => ({ OrientationGizmo: class {
    render = gpu.gizmoRender;
    dispose = gpu.gizmoDispose;
} }));

describe("ModelScene resource ownership", () => {
    let resize: ResizeObserverCallback;
    let scene: ModelScene;
    const host = { clientWidth: 640, clientHeight: 360, appendChild: vi.fn() };
    const triangle = { sourceId: 1, positions: [0, 0, 0, 10, 0, 0, 0, 10, 0], indices: [0, 1, 2] };

    beforeEach(() => {
        vi.clearAllMocks();
        vi.stubGlobal("window", { devicePixelRatio: 1 });
        vi.stubGlobal("ResizeObserver", class {
            constructor(callback: ResizeObserverCallback) { resize = callback; }
            observe = vi.fn(); disconnect = gpu.disconnect;
        });
        scene = new ModelScene(host as unknown as HTMLElement);
    });
    afterEach(() => { scene.dispose(); vi.restoreAllMocks(); vi.unstubAllGlobals(); });

    const layered = {
        sourceId: 5,
        positions: [0,0,0, 1,0,0, 0,1,0, 0,0,5, 1,0,5, 0,1,5, 0,0,0, 1,0,0, 0,1,0],
        indices: [0,1,2,3,4,5,6,7,8],
        opacities: Array(9).fill(0.5),
    };

    it("draws the optional orientation gizmo after the scene without clearing the scene color", () => {
        gpu.gizmoRender.mockImplementation((renderer: THREE.WebGLRenderer) => expect(renderer.autoClear).toBe(false));
        scene.setGeometry(triangle);
        renderAt(0);
        expect(gpu.gizmoRender).not.toHaveBeenCalled();
        scene.showOrientationGizmo();
        renderAt(16);
        expect(gpu.gizmoRender).toHaveBeenCalledOnce();
        expect(gpu.gizmoRender.mock.invocationCallOrder[0]).toBeGreaterThan(gpu.render.mock.invocationCallOrder.at(-1)!);
        expect(gpu.gizmoRender.mock.lastCall![0].autoClear).toBe(true);
        scene.dispose();
        expect(gpu.gizmoDispose).toHaveBeenCalledOnce();
    });
    function faceOrder(mesh: THREE.Mesh<THREE.BufferGeometry>): number[] {
        return mesh.geometry.groups.flatMap(group => Array.from({length: group.count / 3}, (_, i) => mesh.geometry.index!.getX(group.start + i * 3) / 3));
    }

    it.each([false,true])("samples first texel independently of scrolling and keeps shader/alpha hooks, HDR=%s", hdr => {
        const image = {width: 4,height: 2} as ImageBitmap;
        scene.setTextures(new Map([[8,{image,alphaMode:1 as const,effectId:1,scrollU:1,...(hdr ? {hdr:{width:4,height:2,rgba:Array(32).fill(1)}} : {})}]]));
        scene.setGeometry({...triangle,textureIds:[8],uvs:[0,0,0,0,0,0],firstTexel:[1,1,1]});
        const mesh=renderAt(0), material=mesh.material[2];
        expect(Array.from(mesh.geometry.getAttribute("cacheFirstTexel").array)).toEqual([1,1,1]);
        const shader={uniforms:{},vertexShader:THREE.ShaderLib.standard.vertexShader,fragmentShader:THREE.ShaderLib.standard.fragmentShader};
        material.onBeforeCompile(shader as unknown as THREE.WebGLProgramParametersWithUniforms,{} as THREE.WebGLRenderer);
        const uniforms=shader.uniforms as Record<string,{value: THREE.Vector2}>;
        expect(uniforms.cacheFirstTexelUv.value.toArray()).toEqual([.125,.75]);
        expect(shader.fragmentShader).toContain("vCacheFirstTexel > 0.5 ? cacheFirstTexelUv : vMapUv");
        expect(shader.fragmentShader).toContain("diffuseColor *= sampledDiffuseColor");
        expect(shader.uniforms).toHaveProperty("cachePhase");
        expect(material.customProgramCacheKey()).toContain("cache-effect-1");
        expect(material).toMatchObject({alphaTest:.5,transparent:false,depthWrite:true});
        renderAt(1000);
        expect(material.map!.offset.x).not.toBe(0);
        expect(uniforms.cacheFirstTexelUv.value.toArray()).toEqual([.125,.75]);
        const dispose=vi.spyOn(mesh.geometry,"dispose");
        scene.setGeometry({...triangle,textureIds:[8],uvs:[0,0,1,0,0,1]});
        expect(dispose).toHaveBeenCalledOnce();
        expect(renderAt(1016).geometry.hasAttribute("cacheFirstTexel")).toBe(false);
    });

    it("batches alternating opaque and cutout materials without changing source triangles or attributes", () => {
        scene.setTextures(new Map([
            [8, { image: {} as ImageBitmap, alphaMode: 0 as const }],
            [9, { image: {} as ImageBitmap, alphaMode: 1 as const }],
        ]));
        const model = { ...layered, opacities: Array(9).fill(1), textureIds: [8,9,8], colors: Array.from({length: 27}, (_, i) => i / 27), uvs: Array.from({length: 18}, (_, i) => i / 18) };
        scene.setGeometry(model);
        const mesh = renderAt(0);
        expect(mesh.geometry.groups).toEqual([{start: 0, count: 6, materialIndex: 2}, {start: 6, count: 3, materialIndex: 4}]);
        expect(Array.from(mesh.geometry.index!.array)).toEqual([0,1,2,6,7,8,3,4,5]);
        expect(model.indices).toEqual([0,1,2,3,4,5,6,7,8]);
        expect(Array.from(mesh.geometry.getAttribute("position").array)).toEqual(model.positions);
        expect(mesh.geometry.getAttribute("color").getX(1)).toBeCloseTo(model.colors[3]);
        expect(mesh.geometry.getAttribute("color").getW(1)).toBe(1);
        expect(Array.from(mesh.geometry.getAttribute("uv").array)).toEqual(Array.from(new Float32Array(model.uvs)));
        const regroup = vi.spyOn(mesh.geometry, "clearGroups");
        scene.orbit(0.2, 0.1); renderAt(16);
        expect(regroup).not.toHaveBeenCalled();
        expect(mesh.material[4]).toMatchObject({alphaTest: 0.5, transparent: false, depthWrite: true});
    });

    it("rebuilds batched groups when animated alpha changes and reuses geometry", () => {
        const opaque = {...layered, opacities: Array(9).fill(1)};
        scene.setGeometry(opaque);
        const mesh = renderAt(0), geometry = mesh.geometry;
        scene.setGeometry(layered);
        expect(renderAt(16).geometry).toBe(geometry);
        expect(faceOrder(mesh)).toEqual([0,2,1]);
        scene.setGeometry(opaque);
        expect(renderAt(32).geometry).toBe(geometry);
        expect(geometry.groups).toEqual([{start: 0, count: 9, materialIndex: 0}]);
        expect(Array.from(geometry.index!.array)).toEqual(layered.indices);
    });

    it("preserves a navigated map camera on visibility updates and reframes a new selection", () => {
        scene.setSceneGeometry(triangle, true);
        renderAt(0);
        const camera = gpu.render.mock.lastCall![1] as THREE.PerspectiveCamera;
        const initial = camera.position.clone();
        scene.orbit(0.5, 0.2);
        expect(camera.position.distanceTo(initial)).toBeGreaterThan(1);
        const orbited = camera.position.clone();
        scene.pan(1, 0);
        expect(camera.position.distanceTo(orbited)).toBeGreaterThan(0);
        scene.zoom(0.8);
        const navigated = camera.position.clone();
        scene.setSceneGeometry({...triangle, positions: triangle.positions.map(value => value * 2)}, false);
        expect(camera.position).toEqual(navigated);
        scene.setSceneGeometry(triangle, true);
        expect(camera.position).toEqual(initial);
    });

    it("frames a flat map tightly without clipping its bounds", () => {
        const map = new ModelScene({ ...host, clientWidth: 792 } as unknown as HTMLElement, "Map", "map");
        try {
            map.setSceneGeometry({ positions: [0, 0, 0, 64, 0, 0, 64, 0, 64, 0, 0, 64], indices: [0, 1, 2, 0, 2, 3] }, true);
            gpu.setAnimationLoop.mock.lastCall![0](0);
            const camera = gpu.render.mock.lastCall![1] as THREE.PerspectiveCamera;
            camera.lookAt(32, 0, 32);
            camera.updateMatrixWorld();
            const corners = [new THREE.Vector3(), new THREE.Vector3(64, 0, 0), new THREE.Vector3(64, 0, 64), new THREE.Vector3(0, 0, 64)];
            const projected = corners.map(corner => corner.project(camera));
            const extent = Math.max(...projected.flatMap(point => [Math.abs(point.x), Math.abs(point.y)]));
            expect(Math.max(...projected.map(point => Math.abs(point.x)))).toBeGreaterThan(0.85);
            expect(extent).toBeLessThan(1);
        } finally {
            map.dispose();
        }
    });

    it("groups blended terrain separately and disposes its material on scene replacement", () => {
        scene.setTextures(new Map([[8, { image: {} as ImageBitmap, alphaMode: 0 as const }]]));
        scene.setGeometry({ ...triangle, textureIds: [-2], terrainWeights: Array(3).fill([1, 0, 0, 0, 0, 0, 0, 0]).flat(),
            terrainBlends: [{ id: -2, layers: [{ textureId: 8, textureScale: 512, color: [0, 1, 0] }] }] });
        const mesh = renderAt(0);
        expect(mesh.geometry.groups).toEqual([{ start: 0, count: 3, materialIndex: 4 }]);
        expect(mesh.geometry.getAttribute("terrainWeightsA").count).toBe(3);
        const release = vi.spyOn(mesh.material[4], "dispose");
        scene.setGeometry({ ...triangle, sourceId: 2 });
        expect(release).toHaveBeenCalledOnce();
        expect(renderAt(16).material).toHaveLength(4);
    });

    it("retains terrain buffers and materials through twenty progressive texture arrivals", () => {
        const model = { ...triangle, sourceId: undefined, preparedLighting: true, textureIds: [-2],
            colors: Array(9).fill(0.5), uvs: [0,0,1,0,0,1],
            terrainWeights: Array(3).fill(Array(8).fill(1 / 8)).flat(),
            terrainBlends: [{ id: -2, layers: Array.from({ length: 8 }, (_, index) => ({ textureId: index + 1, textureScale: 512, color: [1,1,1] })) }] };
        scene.setGeometry(model);
        const initial = renderAt(0), geometry = initial.geometry, material = initial.material[2];
        const positions = geometry.getAttribute("position"), weights = geometry.getAttribute("terrainWeightsA");
        const releaseGeometry = vi.spyOn(geometry, "dispose"), releaseMaterial = vi.spyOn(material, "dispose");
        const shader = { uniforms: {} as Record<string, { value: unknown }>, vertexShader: THREE.ShaderLib.basic.vertexShader, fragmentShader: THREE.ShaderLib.basic.fragmentShader };
        material.onBeforeCompile(shader as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
        const program = material.customProgramCacheKey();
        const images = new Map<number, ModelTexture>();
        for (let id = 1; id <= 20; id++) {
            images.set(id, { image: {} as ImageBitmap, alphaMode: 0 });
            scene.setTextures(new Map(images));
            scene.setGeometry(model);
            const mesh = renderAt(id * 16);
            expect(mesh.visible).toBe(true);
            expect(mesh.geometry).toBe(geometry);
            expect(mesh.geometry.getAttribute("position")).toBe(positions);
            expect(mesh.geometry.getAttribute("terrainWeightsA")).toBe(weights);
            expect(mesh.material[mesh.geometry.groups[0].materialIndex ?? 0]).toBe(material);
            expect(material.customProgramCacheKey()).toBe(program);
            expect(shader.uniforms.floorMapEnabled0.value).toBe(true);
        }
        expect(releaseGeometry).not.toHaveBeenCalled();
        expect(releaseMaterial).not.toHaveBeenCalled();
        // Landscape growth requires new buffers but must preserve the existing terrain program.
        scene.setGeometry({ ...model, positions: model.positions.map(value => value + 1) });
        expect(releaseGeometry).toHaveBeenCalledOnce();
        expect(releaseMaterial).not.toHaveBeenCalled();
        const grown = renderAt(400);
        expect(grown.material[grown.geometry.groups[0].materialIndex ?? 0]).toBe(material);
        scene.setGeometry(triangle);
        expect(releaseMaterial).toHaveBeenCalledOnce();
        scene.dispose();
        expect(releaseMaterial).toHaveBeenCalledOnce();
    });

    it("reuses live effect materials and retires replaced image bindings once", () => {
        const water: ModelTexture = { image: {} as ImageBitmap, alphaMode: 0, effectId: 4, effectParameter: 19 };
        const images = new Map([[8, water]]);
        const model = { ...triangle, preparedLighting: true, colors: Array(9).fill(0.5), textureIds: [8], uvs: [0,0,1,0,0,1] };
        scene.setTextures(images); scene.setGeometry(model);
        const material = renderAt(0).material[2];
        const release = vi.spyOn(material, "dispose");
        const shader = { uniforms: {} as Record<string, { value: THREE.Vector4 }>, vertexShader: "#include <begin_vertex>", fragmentShader: "#include <opaque_fragment>" };
        material.onBeforeCompile(shader as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
        images.set(9, { image: {} as ImageBitmap, alphaMode: 0 });
        scene.setTextures(new Map(images)); scene.setGeometry(model);
        expect(renderAt(0).material[2]).toBe(material);
        const initial = shader.uniforms.cachePhase.value.clone();
        renderAt(1000);
        expect(shader.uniforms.cachePhase.value).not.toEqual(initial);
        expect(release).not.toHaveBeenCalled();
        images.set(8, { ...water, image: {} as ImageBitmap, alphaMode: 1 });
        scene.setTextures(new Map(images)); scene.setGeometry(model);
        const replacedMesh = renderAt(2000);
        const replacement = replacedMesh.material[replacedMesh.geometry.groups[0].materialIndex ?? 0];
        expect(replacement).not.toBe(material);
        expect(replacement.alphaTest).toBe(0.5);
        expect(release).toHaveBeenCalledOnce();
        const previous = shader.uniforms.cachePhase.value.clone();
        renderAt(3000);
        expect(shader.uniforms.cachePhase.value).toEqual(previous);
        scene.dispose(); expect(release).toHaveBeenCalledOnce();
    });

    it("orders blended faces by client priority before depth including priorities 10 and 11", () => {
        scene.setGeometry({...layered, facePriorities: [11, 10, 0]});
        const mesh = renderAt(0);
        expect(faceOrder(mesh)).toEqual([2, 1, 0]);
        expect(layered.indices).toEqual([0,1,2,3,4,5,6,7,8]);
        expect(Array.from(mesh.geometry.index!.array).sort((a,b) => a-b)).toEqual(layered.indices);
        expect(mesh.material[1]).toMatchObject({transparent: true, depthWrite: false, depthTest: true});
    });

    it("reorders equal-priority faces on camera reversal and retains source order for depth ties", () => {
        scene.setGeometry(layered);
        const mesh = renderAt(0);
        expect(faceOrder(mesh)).toEqual([0, 2, 1]);
        expect(mesh.geometry.groups).toEqual([{ start: 0, count: 9, materialIndex: 1 }]);
        const index = mesh.geometry.index!;
        const version = index.version;
        const camera = gpu.render.mock.lastCall![1] as THREE.PerspectiveCamera;
        camera.position.addScalar(1);
        renderAt(8);
        expect(index.version).toBe(version);
        camera.position.set(0, 0, -20); camera.lookAt(0, 0, 0);
        renderAt(16);
        expect(faceOrder(mesh)).toEqual([1, 0, 2]);
        expect(index.version).toBeGreaterThan(version);
        expect(mesh.geometry.groups).toEqual([{ start: 0, count: 9, materialIndex: 1 }]);
        expect(layered.indices).toEqual([0,1,2,3,4,5,6,7,8]);
    });

    it("batches only adjacent transparent material runs without crossing client priorities", () => {
        scene.setTextures(new Map([
            [8, { image: {} as ImageBitmap, alphaMode: 2 as const }],
            [9, { image: {} as ImageBitmap, alphaMode: 2 as const }],
        ]));
        const model = { ...layered, textureIds: [8,9,8] };
        scene.setGeometry(model);
        const mesh = renderAt(0);
        expect(faceOrder(mesh)).toEqual([0,2,1]);
        expect(mesh.geometry.groups).toEqual([
            { start: 0, count: 6, materialIndex: 3 },
            { start: 6, count: 3, materialIndex: 5 },
        ]);
        scene.setGeometry({ ...model, facePriorities: [0,0,1] });
        renderAt(16);
        expect(faceOrder(mesh)).toEqual([0,1,2]);
        expect(mesh.geometry.groups.map(group => group.materialIndex)).toEqual([3,5,3]);
        expect(model.indices).toEqual([0,1,2,3,4,5,6,7,8]);
    });

    it("keeps the opaque index prefix fixed while batching camera-sorted transparent faces", () => {
        const model = { ...layered, opacities: [1,1,1,0.5,0.5,0.5,0.5,0.5,0.5] };
        scene.setGeometry(model);
        const mesh = renderAt(0);
        expect(faceOrder(mesh)).toEqual([0,2,1]);
        const prefix = Array.from(mesh.geometry.index!.array.slice(0, 3));
        expect(mesh.geometry.groups).toEqual([
            { start: 0, count: 3, materialIndex: 0 },
            { start: 3, count: 6, materialIndex: 1 },
        ]);
        const camera = gpu.render.mock.lastCall![1] as THREE.PerspectiveCamera;
        camera.position.set(0,0,-20); camera.lookAt(0,0,0);
        renderAt(16);
        expect(faceOrder(mesh)).toEqual([0,1,2]);
        expect(Array.from(mesh.geometry.index!.array.slice(0, 3))).toEqual(prefix);
        expect(model.indices).toEqual([0,1,2,3,4,5,6,7,8]);
    });

    it("updates animated alpha and depth without replacing geometry or altering source topology", () => {
        scene.setGeometry(layered);
        const mesh = renderAt(0), geometry = mesh.geometry;
        const release = vi.spyOn(geometry, "dispose");
        scene.setGeometry({...layered, opacities: [1,1,1,0.5,0.5,0.5,0.5,0.5,0.5], facePriorities: [11, 10, 0]});
        expect(renderAt(16).geometry).toBe(geometry);
        expect(faceOrder(mesh)).toEqual([0, 2, 1]);
        expect(geometry.groups[0].materialIndex).toBe(0);
        expect(release).not.toHaveBeenCalled();
        expect(layered.indices).toEqual([0,1,2,3,4,5,6,7,8]);
        expect(Array.from(geometry.index!.array).sort((a,b) => a-b)).toEqual(layered.indices);
        scene.setGeometry(triangle);
        expect(faceOrder(renderAt(32))).toEqual([0]);
        expect(release).toHaveBeenCalledTimes(1);
    });

    it("keeps cutout faces opaque and orders texture-alpha faces with face-alpha faces", () => {
        scene.setTextures(new Map([
            [8, {image: {} as ImageBitmap, alphaMode: 1 as const}],
            [9, {image: {} as ImageBitmap, alphaMode: 2 as const}],
        ]));
        scene.setGeometry({...layered, textureIds: [9, 8, -1], opacities: [1,1,1,1,1,1,0.5,0.5,0.5], facePriorities: [11,0,10]});
        const mesh = renderAt(0);
        expect(faceOrder(mesh)).toEqual([1,2,0]);
        expect(mesh.geometry.groups.map(group => group.materialIndex)).toEqual([2,1,4]);
        expect(mesh.material[2]).toMatchObject({alphaTest: 0.5, transparent: false, depthWrite: true});
        expect(mesh.material[4]).toMatchObject({transparent: true, depthWrite: false});
    });

    it("renders client-lit colours once and preserves procedural effect shader hooks", () => {
        scene.setGeometry({...triangle,preparedLighting:true,colors:Array(9).fill(0.5)});
        expect(renderAt(0).material[0]).toBeInstanceOf(THREE.MeshBasicMaterial);
        scene.setTextures(new Map([[8,{image:{} as ImageBitmap,alphaMode:0,effectId:1}]]));
        scene.setGeometry({...triangle,preparedLighting:true,colors:Array(9).fill(0.5),textureIds:[8]});
        const material = renderAt(16).material[2];
        expect(material).toBeInstanceOf(THREE.MeshStandardMaterial);
        const shader = {uniforms:{},vertexShader:THREE.ShaderLib.standard.vertexShader,fragmentShader:THREE.ShaderLib.standard.fragmentShader};
        material.onBeforeCompile(shader as unknown as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
        expect(shader.fragmentShader.indexOf("outgoingLight = diffuseColor.rgb;")).toBeLessThan(shader.fragmentShader.indexOf("outgoingLight = mix(outgoingLight, cacheSky"));
        expect(shader.fragmentShader).toContain("cacheReflection");
    });

    it("updates animation positions without replacing the mesh resources", () => {
        const releaseGeometry = vi.spyOn(THREE.BufferGeometry.prototype, "dispose");
        const releaseMaterial = vi.spyOn(THREE.MeshStandardMaterial.prototype, "dispose");
        scene.setGeometry(triangle);
        scene.setGeometry({ ...triangle, positions: [0, 0, 0, 20, 0, 0, 0, 10, 0] });
        expect(releaseGeometry).not.toHaveBeenCalled();
        expect(releaseMaterial).not.toHaveBeenCalled();
        scene.dispose();
        expect(releaseGeometry).toHaveBeenCalledTimes(1);
        expect(releaseMaterial).toHaveBeenCalledTimes(2);
    });

    it("releases replaced textures and geometry, then disposes the canvas exactly once", () => {
        const releaseTexture = vi.spyOn(THREE.Texture.prototype, "dispose");
        const releaseGeometry = vi.spyOn(THREE.BufferGeometry.prototype, "dispose");
        const textures = new Map<number, ModelTexture>([[8, { image: {} as ImageBitmap, alphaMode: 1 }]]);
        scene.setTextures(textures);
        scene.setGeometry(triangle);
        scene.setTextures(textures);
        expect(releaseTexture).not.toHaveBeenCalled();
        scene.setTextures(new Map());
        expect(releaseTexture).toHaveBeenCalledTimes(1);
        expect(releaseGeometry).not.toHaveBeenCalled();
        scene.dispose(); scene.dispose();
        expect(releaseGeometry).toHaveBeenCalledTimes(1);
        expect(gpu.setAnimationLoop).toHaveBeenLastCalledWith(null);
        expect(gpu.disconnect).toHaveBeenCalledTimes(1);
        expect(gpu.controlsDispose).toHaveBeenCalledTimes(1);
        expect(gpu.dispose).toHaveBeenCalledTimes(1);
        expect(gpu.remove).toHaveBeenCalledTimes(1);
    });

    it("uses a declared floor texture's fallback until its image arrives without changing source geometry", () => {
        const model = { ...triangle, preparedLighting: true, textureIds: [8], colors: Array(9).fill(0.5), uvs: [0,0,1,0,0,1] };
        scene.setGeometry(model);
        const pending = renderAt(0);
        expect(pending.geometry.groups[0].materialIndex).toBe(0);
        expect(pending.material[0].map).toBeNull();
        scene.setTextures(new Map([[8, { image: {} as ImageBitmap, alphaMode: 0 }]]));
        scene.setGeometry(model);
        const loaded = renderAt(1);
        expect(loaded.geometry.groups[0].materialIndex).toBe(2);
        expect(loaded.material[2].map).toBeInstanceOf(THREE.Texture);
        expect(model.textureIds).toEqual([8]);
        expect(Array.from(loaded.geometry.getAttribute("position").array)).toEqual(model.positions);
        scene.setTextures(new Map());
        scene.setGeometry(model);
        expect(renderAt(2).geometry.groups[0].materialIndex).toBe(0);
    });

    it("retains unchanged GPU textures across progressive additions and disposes only replacements", () => {
        const grass: ModelTexture = { image: {} as ImageBitmap, alphaMode: 0 };
        const path: ModelTexture = { image: {} as ImageBitmap, alphaMode: 0 };
        scene.setTextures(new Map([[8, grass]]));
        scene.setGeometry({ ...triangle, textureIds: [8] });
        const original = renderAt(0).material[2].map;
        if (!original) throw new Error("Expected grass texture");
        const release = vi.spyOn(original, "dispose");
        const geometry = renderAt(1).geometry;
        scene.setTextures(new Map([[8, grass]]));
        expect(renderAt(2).geometry).toBe(geometry);
        scene.setTextures(new Map([[8, grass], [9, path]]));
        scene.setGeometry({ ...triangle, textureIds: [8] });
        expect(renderAt(3).material[2].map).toBe(original);
        const pathTexture = renderAt(4).material[4].map;
        expect(release).not.toHaveBeenCalled();
        scene.setTextures(new Map([[8, { ...grass, image: {} as ImageBitmap }], [9, path]]));
        scene.setGeometry({ ...triangle, textureIds: [9] });
        expect(release).toHaveBeenCalledOnce();
        const replaced = renderAt(5);
        expect(replaced.material[replaced.geometry.groups[0].materialIndex ?? 0].map).toBe(pathTexture);
        scene.setTextures(new Map([[9, path]]));
        scene.setGeometry({ ...triangle, textureIds: [9] });
        expect(renderAt(6).material[2].map).toBe(pathTexture);
    });

    it("resizes the drawing buffer when its host changes size", () => {
        host.clientWidth = 320;
        resize([], {} as ResizeObserver);
        expect(gpu.setSize).toHaveBeenLastCalledWith(320, 360);
        host.clientWidth = 640;
    });

    it("applies material repeat and clamp modes independently for both texture axes", () => {
        const released: {u: number; v: number}[] = [];
        vi.spyOn(THREE.Texture.prototype, "dispose").mockImplementation(function(this: THREE.Texture) {
            released.push({u: this.wrapS, v: this.wrapT});
        });
        scene.setTextures(new Map([[8, {image: {} as ImageBitmap, alphaMode: 0, repeatU: false, repeatV: true}]]));
        scene.setTextures(new Map());
        expect(released).toEqual([{u: THREE.ClampToEdgeWrapping, v: THREE.RepeatWrapping}]);
    });

    function renderAt(time: number) {
        gpu.setAnimationLoop.mock.calls[0][0](time);
        const rendered = gpu.render.mock.lastCall![0] as THREE.Scene;
        return rendered.children.find(child => child instanceof THREE.Mesh) as THREE.Mesh<THREE.BufferGeometry, THREE.MeshStandardMaterial[]>;
    }

    it("toggles the grid and wireframe across mesh replacement and disposes helpers", () => {
        scene.setDisplay(true, true);
        scene.setGeometry(triangle);
        expect(renderAt(0).material.every(material => material.wireframe)).toBe(true);
        const rendered = gpu.render.mock.lastCall![0] as THREE.Scene;
        const grid = rendered.children.find(child => child instanceof THREE.GridHelper) as THREE.GridHelper;
        expect(grid.visible).toBe(true);
        expect(grid.scale.x).toBeGreaterThan(1);
        scene.setGeometry({...triangle, sourceId: 2});
        expect(renderAt(1).material.every(material => material.wireframe)).toBe(true);
        scene.setDisplay(false, false);
        expect(grid.visible).toBe(false);
        expect(renderAt(2).material.some(material => material.wireframe)).toBe(false);
        const geometry = vi.spyOn(grid.geometry, "dispose");
        const material = vi.spyOn(grid.material as THREE.Material, "dispose");
        scene.dispose();
        expect(geometry).toHaveBeenCalledOnce(); expect(material).toHaveBeenCalledOnce();
    });

    it("scrolls on the existing loop, keeps time during posing and resets it on model replacement", () => {
        scene.setTextures(new Map([[8, {image: {} as ImageBitmap, alphaMode: 0, scrollU: -32, scrollV: 16}]]));
        scene.setGeometry({...triangle, textureIds: [8]});
        renderAt(5000);
        const mesh = renderAt(6000);
        expect(mesh.material[2].map!.offset.toArray()).toEqual([-0.5, 0.25]);
        scene.setGeometry({...triangle, textureIds: [8], positions: triangle.positions.map(value => value + 1)});
        expect(renderAt(6500).material[2].map!.offset.toArray()).toEqual([-0.75, 0.375]);
        scene.setGeometry({...triangle, sourceId: 2, textureIds: [8]});
        expect(renderAt(7000).material[2].map!.offset.toArray()).toEqual([-0, 0]);
        expect(gpu.setAnimationLoop).toHaveBeenCalledTimes(1);
    });

    it("creates unlit effect surfaces while preserving cutout and face transparency", () => {
        scene.setTextures(new Map([[8, {image: {} as ImageBitmap, alphaMode: 1, effectId: 6}]]));
        scene.setGeometry({...triangle, textureIds: [8], opacities: [0.5, 0.5, 0.5]});
        const mesh = renderAt(0);
        expect(mesh.material[2]).toBeInstanceOf(THREE.MeshBasicMaterial);
        expect(mesh.material[2]).toMatchObject({alphaTest: 0.5, transparent: false, depthWrite: true});
        expect(mesh.material[3]).toMatchObject({alphaTest: 0.5, transparent: true, depthWrite: false});
        expect(mesh.geometry.groups[0].materialIndex).toBe(3);
    });

    it("uploads HDR rows in bitmap orientation and releases float textures on replacement", () => {
        const release = vi.spyOn(THREE.DataTexture.prototype, "dispose");
        scene.setTextures(new Map([[8, {image: {} as ImageBitmap, alphaMode: 2, hdr: {width: 1, height: 2, rgba: [16.5, 0, 0, 1, 0, 2, 0, 0.5]}}]]));
        scene.setGeometry({...triangle, textureIds: [8]});
        const texture = renderAt(0).material[2].map as THREE.DataTexture;
        expect(Array.from(texture.image.data)).toEqual([0, 2, 0, 0.5, 16.5, 0, 0, 1]);
        expect(texture.colorSpace).toBe(THREE.LinearSRGBColorSpace);
        scene.setTextures(new Map());
        expect(release).toHaveBeenCalledTimes(1);
    });

    it("animates attached particles on the existing loop and releases them on model replacement", () => {
        const definition = {textureId: -1, colorMin: 0xffffffff, colorMax: 0xffffffff, lifeMin: 0.05, lifeMax: 0.06, rateMin: 500, rateMax: 500, sizeMin: 40, sizeMax: 48, speedMin: 0, speedMax: 0, angles: [0,0,0,0], unsupportedOpcodes: []};
        scene.setGeometry({...triangle, particles: [{emitterId: 403, face: 0, definition}]});
        renderAt(0); renderAt(16);
        const rendered = gpu.render.mock.lastCall![0] as THREE.Scene;
        const points = rendered.children.find(child => child instanceof THREE.Points) as THREE.Points<THREE.BufferGeometry,THREE.ShaderMaterial>;
        expect(points.geometry.getAttribute("particleColor").getW(0)).toBeGreaterThan(0);
        const releaseGeometry = vi.spyOn(points.geometry,"dispose"), releaseMaterial = vi.spyOn(points.material,"dispose");
        scene.setGeometry({...triangle,sourceId: 2}); renderAt(32);
        expect(rendered.children.some(child => child instanceof THREE.Points)).toBe(false);
        expect(releaseGeometry).toHaveBeenCalledTimes(1); expect(releaseMaterial).toHaveBeenCalledTimes(1);
        expect(gpu.setAnimationLoop).toHaveBeenCalledTimes(1);
    });

    it.each([1, 2, 4, 5, 7, 8, 9])("binds deterministic shader parameters for effect %s and clears old updates", id => {
        scene.setTextures(new Map([[8, {image: {} as ImageBitmap, alphaMode: 0, effectId: id, effectParameter: 19, effectParameter2: 1}]]));
        scene.setGeometry({...triangle, textureIds: [8]});
        const material = renderAt(0).material[2];
        const shader = {uniforms: {}, vertexShader: THREE.ShaderLib.standard.vertexShader, fragmentShader: THREE.ShaderLib.standard.fragmentShader};
        material.onBeforeCompile(shader as unknown as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
        renderAt(2500);
        const uniforms = shader.uniforms as Record<string, {value: THREE.Vector4}>;
        expect(uniforms.cachePhase.value.x).toBe(id === 4 ? 0.625 : id === 5 ? 0.75 : id >= 8 ? 0.5 : 0.625);
        expect(shader.fragmentShader).toContain("uniform vec4 cachePhase");
        if ([1,7,8,9].includes(id)) expect(shader.fragmentShader).toContain("cacheReflection");
        if ([2,4,5,8,9].includes(id)) expect(shader.fragmentShader).toContain("cachePoint");
        const previous = uniforms.cachePhase.value.clone();
        scene.setTextures(new Map()); renderAt(3000);
        expect(uniforms.cachePhase.value).toEqual(previous);
    });
});
