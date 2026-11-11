import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import * as THREE from "three";
import { ModelScene, type ModelTexture } from "./model-scene";
import { waterVolume } from "./water-noise";

function required<T>(value: T | undefined, what: string): T {
    if (value === undefined) throw new Error(`Missing ${what}`);
    return value;
}

const gpu = vi.hoisted(() => ({
    setSize: vi.fn(), setAnimationLoop: vi.fn(), dispose: vi.fn(), remove: vi.fn(),
    disconnect: vi.fn(), controlsDispose: vi.fn(), render: vi.fn(),
    controlsListen: vi.fn(), controlsUnlisten: vi.fn(), controlsCreated: vi.fn(),
    gizmoRender: vi.fn(), gizmoDispose: vi.fn(),
    placementShow: vi.fn(), placementDispose: vi.fn(), placementCreated: vi.fn(), placementFit: vi.fn(),
}));

vi.mock("three", async importOriginal => {
    const actual = await importOriginal<typeof import("three")>();
    return { ...actual, WebGLRenderer: class {
        autoClear = true;
        domElement = { setAttribute: vi.fn(), addEventListener: vi.fn(), remove: gpu.remove, toDataURL: () => "data:image/png;base64,frame" };
        setPixelRatio = vi.fn(); setSize = gpu.setSize; setAnimationLoop = gpu.setAnimationLoop;
        render = gpu.render; dispose = gpu.dispose;
    } };
});
vi.mock("three/addons/controls/OrbitControls.js", () => ({ OrbitControls: class {
    target = new THREE.Vector3(); update = vi.fn(); dispose = gpu.controlsDispose;
    zoomToCursor = false;
    mouseButtons = {}; touches = {};
    addEventListener = gpu.controlsListen; removeEventListener = gpu.controlsUnlisten;
    constructor() { gpu.controlsCreated(this); }
} }));
vi.mock("./orientation-gizmo", () => ({ OrientationGizmo: class {
    render = gpu.gizmoRender;
    dispose = gpu.gizmoDispose;
} }));

vi.mock("./placement-gizmo", () => ({ PlacementGizmo: class {
    readonly helper = new THREE.Object3D();
    readonly dragging = true;
    show = gpu.placementShow;
    fitToCamera = gpu.placementFit;
    dispose = gpu.placementDispose;
    constructor() { gpu.placementCreated(this); }
} }));

describe("ModelScene resource ownership", () => {
    let resize: ResizeObserverCallback;
    let scene: ModelScene;
    const host = { clientWidth: 640, clientHeight: 360, appendChild: vi.fn(), getBoundingClientRect: () => ({ left: 0, top: 0, width: 640, height: 360 }) };
    const triangle = { sourceId: 1, positions: [0, 0, 0, 10, 0, 0, 0, 10, 0], indices: [0, 1, 2] };

    beforeEach(() => {
        vi.clearAllMocks();
        vi.stubGlobal("window", { devicePixelRatio: 1 });
        vi.stubGlobal("requestAnimationFrame", vi.fn((_callback: FrameRequestCallback) => 1));
        vi.stubGlobal("cancelAnimationFrame", vi.fn());
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

    it("updates animated map buffers in place and restores the base pose when switched off", () => {
        vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
        const map = new ModelScene(host as unknown as HTMLElement, "Map", "map");
        try {
            const rgba = new Float32Array(12).fill(1);
            const positions = Float32Array.from(triangle.positions, value => value + 3);
            const sample = vi.fn(() => ({ delay: 20, pose: { positions, rgba: new Float32Array(12).fill(0.5) } }));
            map.setSceneGeometry({ terrain: null, objects: {
                ...triangle, vertexRgba: rgba, preparedLighting: true,
                animations: [{ startVertex: 0, vertexCount: 3, alphaFaces: [true], sample }],
            } }, false);
            map.captureImage();
            const mesh = (gpu.render.mock.lastCall![0] as THREE.Scene).children.find(child => child instanceof THREE.Mesh) as THREE.Mesh;
            expect(mesh.geometry.getAttribute("position").getX(0)).toBe(3);
            expect(mesh.geometry.getAttribute("color").getW(0)).toBe(0.5);
            expect(rgba[3]).toBe(1);
            expect(vi.getTimerCount()).toBe(1);
            map.setObjectAnimationsEnabled(false);
            map.captureImage();
            expect(mesh.geometry.getAttribute("position").getX(0)).toBe(0);
            expect(mesh.geometry.getAttribute("color").getW(0)).toBe(1);
            expect(sample).toHaveBeenCalledTimes(1);
            expect(vi.getTimerCount()).toBe(0);
            map.setObjectAnimationsEnabled(true);
            map.captureImage();
            expect(vi.getTimerCount()).toBe(1);
        } finally {
            map.dispose();
            const timers = vi.getTimerCount();
            vi.useRealTimers();
            expect(timers).toBe(0);
        }
    });

    it("renders camera zooms between animation frames without resampling object poses", () => {
        vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
        const map = new ModelScene(host as unknown as HTMLElement, "Map", "map");
        try {
            const sample = vi.fn(() => ({ delay: 40 }));
            map.setSceneGeometry({ terrain: null, objects: {
                ...triangle, vertexRgba: new Float32Array(12).fill(1),
                animations: [{ startVertex: 0, vertexCount: 3, alphaFaces: [false], sample }],
            } }, true);
            const frame = (time: number) => vi.mocked(requestAnimationFrame).mock.lastCall![0](time);
            frame(0);
            expect(sample).toHaveBeenCalledExactlyOnceWith(0, undefined);
            const camera = gpu.render.mock.lastCall![1] as THREE.PerspectiveCamera;
            const before = camera.position.clone();

            map.zoom(0.8);
            frame(10);
            expect(camera.position).not.toEqual(before);
            expect(gpu.render).toHaveBeenCalledTimes(2);
            expect(sample).toHaveBeenCalledTimes(1);
            expect(vi.getTimerCount()).toBe(1);

            vi.advanceTimersByTime(40);
            frame(40);
            expect(sample).toHaveBeenLastCalledWith(40, 0);
            expect(gpu.render).toHaveBeenCalledTimes(3);
        } finally {
            map.dispose();
            vi.useRealTimers();
        }
    });

    it("creates placement handles on first use, keeps them out of saved images and releases them", () => {
        scene.showPlacementGizmo(null);
        expect(gpu.placementCreated).not.toHaveBeenCalled();
        expect(scene.placementGizmoDragging).toBe(false);

        const target = { mode: "translate", range: { startVertex: 0, endVertex: 3 }, tile: { x: 1, y: 2, plane: 0 }, scale: 1, snap: 1, onRelease: vi.fn() } as const;
        scene.showPlacementGizmo(target);
        expect(gpu.placementShow).toHaveBeenCalledWith(target);
        expect(scene.placementGizmoDragging).toBe(true);

        const { helper } = gpu.placementCreated.mock.lastCall![0] as { helper: THREE.Object3D };
        let visibleInImage: boolean | undefined;
        gpu.render.mockImplementationOnce(() => { visibleInImage = helper.visible; });
        scene.captureImage();
        expect(visibleInImage).toBe(false);
        expect(gpu.placementFit.mock.invocationCallOrder[0]).toBeLessThan(gpu.render.mock.invocationCallOrder.at(-1)!);
        expect(helper.visible).toBe(true);

        scene.dispose();
        expect(gpu.placementDispose).toHaveBeenCalledOnce();
    });

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

    it("picks the visible source vertices behind a fully transparent face after sorting", () => {
        const vertexRgba = new Float32Array(24).fill(1);
        vertexRgba[3] = vertexRgba[7] = vertexRgba[11] = 0;
        const uvs = new Float32Array(12);
        scene.setGeometry({
            positions: [0, 0, 4, 10, 0, 4, 0, 10, 4, ...triangle.positions],
            indices: [0, 1, 2, 3, 4, 5],
            vertexRgba,
            uvs,
        });
        const { geometry } = renderAt(0);
        expect(geometry.getAttribute("color").array).toBe(vertexRgba);
        expect(geometry.getAttribute("uv").array).toBe(uvs);
        const camera = gpu.render.mock.lastCall![1] as THREE.PerspectiveCamera;
        camera.position.set(2, 2, 10);
        camera.lookAt(2, 2, 0);
        expect(scene.pickVertex(320, 180)).toBe(3);
        expect(scene.pickVertex(639, 1)).toBeNull();
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

    it("anchors wheel zoom to the pointer in map scenes", () => {
        expect(gpu.controlsCreated.mock.lastCall![0].zoomToCursor).toBe(false);
        const map = new ModelScene(host as unknown as HTMLElement, "Map", "map");
        try {
            expect(gpu.controlsCreated.mock.lastCall![0].zoomToCursor).toBe(true);
        } finally {
            map.dispose();
        }
    });

    it("preserves a navigated map camera on visibility updates and reframes a new selection", () => {
        scene.setSceneGeometry({ terrain: null, objects: triangle }, true);
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
        scene.setSceneGeometry({ terrain: null, objects: {...triangle, positions: triangle.positions.map(value => value * 2)} }, false);
        expect(camera.position).toEqual(navigated);
        scene.setSceneGeometry({ terrain: null, objects: triangle }, true);
        expect(camera.position).toEqual(initial);
    });

    it("frames a flat map tightly without clipping its bounds", () => {
        const map = new ModelScene({ ...host, clientWidth: 792 } as unknown as HTMLElement, "Map", "map");
        try {
            map.setSceneGeometry({ terrain: null, objects: { positions: [0, 0, 0, 64, 0, 0, 64, 0, 64, 0, 0, 64], indices: [0, 1, 2, 0, 2, 3] } }, true);
            vi.mocked(requestAnimationFrame).mock.lastCall![0](0);
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

    it("renders maps on demand and coalesces scene, camera and resize changes", () => {
        gpu.setAnimationLoop.mockClear();
        const frames = vi.mocked(requestAnimationFrame);
        const map = new ModelScene(host as unknown as HTMLElement, "Map", "map");
        try {
            map.setSceneGeometry({ terrain: null, objects: { ...triangle, firstTexel: Uint8Array.of(1, 0, 0) } }, true);
            map.setDisplay(true, false);
            expect(gpu.setAnimationLoop).not.toHaveBeenCalled();
            expect(frames).toHaveBeenCalledOnce();
            frames.mock.lastCall![0](0);
            expect(gpu.render).toHaveBeenCalledOnce();
            expect(frames).toHaveBeenCalledOnce();
            const mesh = (gpu.render.mock.lastCall![0] as THREE.Scene).children.find(child => child instanceof THREE.Mesh) as THREE.Mesh;
            expect(mesh.geometry.getAttribute("cacheFirstTexel").array).toBeInstanceOf(Uint8Array);

            gpu.controlsListen.mock.lastCall![1]();
            map.showVertexBounds({ startVertex: 0, endVertex: 3 });
            resize([], {} as ResizeObserver);
            expect(frames).toHaveBeenCalledTimes(2);
            frames.mock.lastCall![0](16);
            expect(gpu.render).toHaveBeenCalledTimes(2);
            expect(frames).toHaveBeenCalledTimes(2);
            map.zoom(0.9);
            expect(frames).toHaveBeenCalledTimes(3);
        } finally {
            map.dispose();
        }
        expect(cancelAnimationFrame).toHaveBeenCalledWith(1);
        expect(gpu.controlsUnlisten).toHaveBeenCalledWith("change", gpu.controlsListen.mock.lastCall![1]);
    });

    it("clears a map to the sky colour, fogs towards it and restores the transparent canvas", () => {
        const frames = vi.mocked(requestAnimationFrame);
        const map = new ModelScene(host as unknown as HTMLElement, "Map", "map");
        try {
            map.setSceneGeometry({ terrain: null, objects: triangle }, true);
            frames.mock.lastCall![0](0);
            const rendered = () => gpu.render.mock.lastCall![0] as THREE.Scene;
            expect(rendered().background).toBeNull();
            expect(rendered().fog).toBeNull();

            map.setEnvironment(0x8da4c2, null);
            frames.mock.lastCall![0](16);
            expect((rendered().background as THREE.Color).getHex()).toBe(0x8da4c2);
            expect(rendered().fog).toBeNull();

            map.setEnvironment(0x8da4c2, { start: 10908, end: 14332 });
            frames.mock.lastCall![0](32);
            const fog = rendered().fog as THREE.Fog;
            expect([fog.color.getHex(), fog.near, fog.far]).toEqual([0x8da4c2, 10908, 14332]);

            map.setEnvironment(null, { start: 10908, end: 14332 });
            frames.mock.lastCall![0](48);
            expect(rendered().background).toBeNull();
            expect(rendered().fog).toBeNull();
            expect(gpu.render).toHaveBeenCalledTimes(4);
        } finally {
            map.dispose();
        }
    });

    it("binds point lights to both map surfaces and toggles them without replacing their geometry", () => {
        const map = new ModelScene(host as unknown as HTMLElement, "Map", "map");
        try {
            const model = { ...triangle, preparedLighting: true, mapPlanes: [2, 2, 2] };
            const field = { width: 1, height: 1, tiles: new Float32Array(16), lights: [{ x: 256, y: 128, z: 256, radius: 256, rgb: 0xc08040 }] };
            field.tiles[8] = 1;
            map.setMapPointLights(field, true);
            map.setSceneGeometry({ terrain: model, objects: model }, false);
            const renderMeshes = () => {
                map.captureImage();
                return (gpu.render.mock.lastCall![0] as THREE.Scene).children.filter(child => child instanceof THREE.Mesh) as THREE.Mesh<THREE.BufferGeometry, THREE.Material[]>[];
            };
            const meshes = renderMeshes();
            expect(meshes).toHaveLength(2);
            const uniforms = meshes.map(mesh => {
                expect(mesh.geometry.getAttribute("mapPlane").getX(0)).toBe(2);
                const shader = { uniforms: {} as Record<string, THREE.IUniform>, vertexShader: THREE.ShaderLib.basic.vertexShader, fragmentShader: THREE.ShaderLib.basic.fragmentShader };
                mesh.material[0].onBeforeCompile(shader as unknown as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
                expect(shader.vertexShader).toContain("mapLightPlane = int(mapPlane)");
                expect(shader.fragmentShader).toContain("#include <colorspace_fragment>\ngl_FragColor.rgb *= 1.0 + mapLocalLight()");
                expect(shader.uniforms.mapLightHeight.value).toBe(1);
                return shader.uniforms;
            });
            const texture = uniforms[0].mapLightTiles.value as THREE.DataTexture;
            const records = uniforms[0].mapLightRecords.value as THREE.DataTexture;
            expect(records.image.data.slice(4, 8)).toEqual(Float32Array.of(192 / 255, 128 / 255, 64 / 255, 0));
            const release = vi.spyOn(texture, "dispose");
            expect(uniforms[1].mapLightTiles.value).toBe(texture);
            map.setMapPointLights(field, false);
            expect(uniforms[0].mapLightHeight.value).toBe(0);
            expect(renderMeshes()).toEqual(meshes);
            expect(release).not.toHaveBeenCalled();
            map.setMapPointLights(null, true);
            expect(release).toHaveBeenCalledOnce();
        } finally {
            map.dispose();
        }
    });

    it("builds one environment cube from source-ordered faces, binds it to reflective effects and releases it", async () => {
        const decoded: { close: ReturnType<typeof vi.fn> }[] = [];
        const decode = vi.fn(async () => { const image = { close: vi.fn() }; decoded.push(image); return image; });
        vi.stubGlobal("createImageBitmap", decode);
        const face = (width: number): ModelTexture => ({ image: { width, height: width } as ImageBitmap, alphaMode: 0 });
        const faces = [face(64), face(128), face(64), face(64), face(64), face(64)];
        scene.setTextures(new Map([[7, { image: {} as ImageBitmap, alphaMode: 0, effectId: 7 }]]));
        scene.setGeometry({ ...triangle, preparedLighting: true, textureIds: [7] });
        const material = renderAt(0).material[2];
        const shader = { uniforms: {} as Record<string, THREE.IUniform>, vertexShader: THREE.ShaderLib.standard.vertexShader, fragmentShader: THREE.ShaderLib.standard.fragmentShader };
        material.onBeforeCompile(shader as unknown as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
        expect(shader.fragmentShader).toContain("textureCube(cacheEnvironment, vec3(-cacheReflection.x, cacheReflection.y, -cacheReflection.z))");
        expect(shader.uniforms.cacheEnvironmentEnabled.value).toBe(false);

        scene.setEnvironment(0xc8c0a8, null, faces);
        scene.setEnvironment(0xc8c0a8, null, [...faces]);
        await new Promise(resolve => setTimeout(resolve));
        expect(decode).toHaveBeenCalledTimes(6);
        expect(decode).toHaveBeenCalledWith(faces[0].image, { imageOrientation: "flipY", resizeWidth: 128, resizeHeight: 128, resizeQuality: "pixelated" });
        const cube = shader.uniforms.cacheEnvironment.value as THREE.CubeTexture;
        expect(cube).toBeInstanceOf(THREE.CubeTexture);
        expect(cube.images).toEqual(decoded);
        expect(cube.colorSpace).toBe(THREE.SRGBColorSpace);
        expect(shader.uniforms.cacheEnvironmentEnabled.value).toBe(true);

        const release = vi.spyOn(cube, "dispose");
        scene.setEnvironment(0xc8c0a8, null, null);
        expect(release).toHaveBeenCalledOnce();
        expect(decoded.every(image => image.close.mock.calls.length === 1)).toBe(true);
        expect(shader.uniforms.cacheEnvironment.value).toBeNull();
        expect(shader.uniforms.cacheEnvironmentEnabled.value).toBe(false);

        scene.setEnvironment(0xc8c0a8, null, faces);
        scene.setEnvironment(0xc8c0a8, null, null);
        await new Promise(resolve => setTimeout(resolve));
        expect(decoded.slice(6).every(image => image.close.mock.calls.length === 1)).toBe(true);
        expect(shader.uniforms.cacheEnvironment.value).toBeNull();
    });

    it("shares a water texture across materials and releases it once per scene", async () => {
        scene.setTextures(new Map([
            [2, { image: {} as ImageBitmap, alphaMode: 0, effectId: 2 }],
            [4, { image: {} as ImageBitmap, alphaMode: 0, effectId: 4 }],
        ]));
        scene.setGeometry({ ...triangle, textureIds: [2] });
        await waterVolume("ridged");
        const materials = renderAt(0).material;
        const noise = (material: THREE.Material) => {
            const shader = { uniforms: {} as Record<string, THREE.IUniform>, vertexShader: THREE.ShaderLib.standard.vertexShader, fragmentShader: THREE.ShaderLib.standard.fragmentShader };
            material.onBeforeCompile(shader as unknown as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
            return shader.uniforms.cacheWaterNoise.value as THREE.Data3DTexture;
        };
        const texture = noise(materials[2]);
        expect(texture).toBeInstanceOf(THREE.Data3DTexture);
        expect(noise(materials[4])).toBe(texture);
        expect(texture.format).toBe(THREE.RGFormat);
        expect([texture.wrapS, texture.wrapT, texture.wrapR]).toEqual(Array(3).fill(THREE.RepeatWrapping));
        expect(texture.minFilter).toBe(THREE.LinearFilter);
        expect(texture.generateMipmaps).toBe(false);
        const dispose = vi.spyOn(texture, "dispose");
        scene.dispose();
        scene.dispose();
        expect(dispose).toHaveBeenCalledOnce();
    });

    it("uses the region cube and texture alpha for effect 7 and the base material for standalone HD water", () => {
        const compile = (effectId: number) => {
            scene.setTextures(new Map([[effectId, { image: {} as ImageBitmap, alphaMode: 0, effectId }]]));
            scene.setGeometry({ ...triangle, preparedLighting: true, textureIds: [effectId] });
            const shader = { uniforms: {}, vertexShader: THREE.ShaderLib.standard.vertexShader, fragmentShader: THREE.ShaderLib.standard.fragmentShader };
            renderAt(0).material[2].onBeforeCompile(shader as unknown as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
            return shader.fragmentShader;
        };
        expect(compile(1)).not.toContain("textureCube(cacheEnvironment");
        const reflection = compile(7);
        const cubeBranch = reflection.match(/if \(cacheEnvironmentEnabled\) \{([^}]+)\}/)?.[1];
        expect(cubeBranch).toContain("textureCube(cacheEnvironment, vec3(-cacheReflection.x, cacheReflection.y, -cacheReflection.z))");
        expect(cubeBranch).toContain("outgoingLight = pow(mix(cacheBaseGamma, cacheCubeGamma, sampledDiffuseColor.a), vec3(2.2));");
        expect(reflection.replace(cubeBranch ?? "", "")).not.toContain("textureCube(cacheEnvironment");
        expect(reflection).not.toContain("cacheSky");
        expect(reflection).not.toContain("cacheFresnel");
        for (const id of [8, 9]) {
            const shader = compile(id);
            expect(shader).not.toContain("cacheSky");
            expect(shader).not.toContain("cacheWaterNoise");
        }
    });

    it("switches HD water on with the region cube and reuses terrain buffers through fallback", async () => {
        const map = new ModelScene(host as unknown as HTMLElement, "Map", "map");
        vi.stubGlobal("createImageBitmap", vi.fn(async () => ({ close: vi.fn() })));
        try {
            const asset = { image: { width: 16, height: 16 } as ImageBitmap, alphaMode: 0, effectId: 9, effectParameter: 19, effectParameter2: 128 } as const;
            map.setTextures(new Map([[9, asset]]));
            map.setSceneGeometry({ terrain: { ...triangle, textureIds: [9], mapPlanes: [0, 0, 0], waterDepth: [0, 95, -1], preparedLighting: true }, objects: null }, false);
            const mesh = () => {
                map.captureImage();
                const rendered = gpu.render.mock.lastCall![0] as THREE.Scene;
                return rendered.children.find(child => child instanceof THREE.Mesh) as THREE.Mesh<THREE.BufferGeometry, THREE.Material[]>;
            };
            const fallback = mesh();
            expect(fallback.material[2].transparent).toBe(false);
            expect(Array.from(fallback.geometry.getAttribute("waterDepth").array)).toEqual([0, 95, -1]);
            map.setEnvironment(0xc8c0a8, { start: 100, end: 500 }, Array(6).fill(asset), {
                lightRgb: 0xff8000, directionX: 0, directionY: -3, directionZ: 4,
            });
            await waterVolume("normal");
            await new Promise(resolve => setTimeout(resolve));
            const water = mesh();
            expect(water.geometry).toBe(fallback.geometry);
            const material = water.material[2];
            expect(material.transparent).toBe(true);
            expect(material.depthWrite).toBe(true);
            const shader = { uniforms: {} as Record<string, THREE.IUniform>, vertexShader: THREE.ShaderLib.basic.vertexShader, fragmentShader: THREE.ShaderLib.basic.fragmentShader };
            material.onBeforeCompile(shader as unknown as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
            expect(shader.uniforms.cacheSunColor.value.toArray()).toEqual([1, 128 / 255, 0]);
            expect(shader.uniforms.cacheSunDirection.value.y).toBeCloseTo(0.6);
            const noise = shader.uniforms.cacheWaterNoise.value as THREE.Data3DTexture;
            expect(noise.format).toBe(THREE.RGBAFormat);
            expect(noise.minFilter).toBe(THREE.LinearMipmapLinearFilter);
            expect(shader.uniforms.cacheWaterParams.value.toArray().slice(1)).toEqual([0.125, 128, 0]);
            expect(shader.fragmentShader).toContain("float waveFactor");
            expect(shader.uniforms.cacheWaterScroll.value).toBeInstanceOf(THREE.Vector2);
            expect(shader.vertexShader).toContain("cacheWaterUv = vec2(uv.x, -uv.y) + cacheWaterScroll");
            expect(shader.fragmentShader).toMatch(/gl_FragColor = clamp\(surfaceColor, 0\.0, 1\.0\);[\s\S]*gl_FragColor\.rgb \*= 1\.0 \+ mapLocalLight\(\);/);
            map.setEnvironment(null, null);
            const restored = mesh();
            expect(restored.geometry).toBe(fallback.geometry);
            expect(restored.material[2].transparent).toBe(false);
        } finally {
            map.dispose();
        }
    });

    it("keeps point lights on fixed water and applies the effect 2 ambient only to terrain", () => {
        const map = new ModelScene(host as unknown as HTMLElement, "Map", "map");
        try {
            map.setTextures(new Map([[2, { image: { width: 16, height: 16 } as ImageBitmap, alphaMode: 0, effectId: 2, effectParameter: 1 }]]));
            const water = { ...triangle, textureIds: [2], mapPlanes: [0, 0, 0], preparedLighting: true };
            map.setSceneGeometry({ terrain: { ...water, waterDepth: [0, 0, 0] }, objects: water }, false);
            map.captureImage();
            const meshes = (gpu.render.mock.lastCall![0] as THREE.Scene).children.filter(child => child instanceof THREE.Mesh) as THREE.Mesh<THREE.BufferGeometry, THREE.Material[]>[];
            const compile = (mesh: THREE.Mesh<THREE.BufferGeometry, THREE.Material[]>) => {
                const shader = { uniforms: {} as Record<string, THREE.IUniform>, vertexShader: THREE.ShaderLib.basic.vertexShader, fragmentShader: THREE.ShaderLib.basic.fragmentShader };
                mesh.material[2].onBeforeCompile(shader as unknown as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
                return shader;
            };
            const terrain = compile(meshes.find(mesh => mesh.geometry.getAttribute("waterDepth"))!);
            const objects = compile(meshes.find(mesh => !mesh.geometry.getAttribute("waterDepth"))!);
            expect(terrain.uniforms.cacheWaterFlags.value.y).toBe(1);
            expect(objects.uniforms.cacheWaterFlags.value.y).toBe(0);
            expect(terrain.fragmentShader).toMatch(/gl_FragColor = waterColor;[\s\S]*gl_FragColor\.rgb \*= 1\.0 \+ mapLocalLight\(\);/);
        } finally {
            map.dispose();
        }
    });

    it("hands a primary drag to the page and restores orbiting", () => {
        const controls = (scene as unknown as { controls: { mouseButtons: { LEFT: unknown }; touches: { ONE: unknown } } }).controls;
        scene.setPrimaryDragEnabled(false);
        expect(controls.mouseButtons.LEFT).toBeNull();
        expect(controls.touches.ONE).toBeNull();
        scene.setPrimaryDragEnabled(true);
        expect(controls.mouseButtons.LEFT).toBe(THREE.MOUSE.ROTATE);
        expect(controls.touches.ONE).toBe(THREE.TOUCH.ROTATE);
    });

    describe("terrain and object surfaces", () => {
        const square = (y: number, sourceId?: number) => ({ positions: [-1, y, -1, 1, y, -1, 0, y, 1], indices: [0, 1, 2], ...(sourceId === undefined ? {} : { sourceId }) });
        const meshes = (rendered: THREE.Scene) => rendered.children.filter(child => child instanceof THREE.Mesh) as THREE.Mesh<THREE.BufferGeometry>[];
        let map: ModelScene;
        const lastScene = () => { gpu.render.mockClear(); map.captureImage(); return gpu.render.mock.lastCall![0] as THREE.Scene; };

        beforeEach(() => { map = new ModelScene(host as unknown as HTMLElement, "Map", "map"); });
        afterEach(() => map.dispose());

        it("draws terrain under objects as separate meshes", () => {
            map.setSceneGeometry({ terrain: square(0), objects: square(5) }, true);
            const [first, second] = meshes(lastScene());
            expect([first.renderOrder, second.renderOrder].sort()).toEqual([-1, 0]);
        });

        it("replaces terrain without touching the object buffers", () => {
            const objects = square(5);
            map.setSceneGeometry({ terrain: square(0), objects }, true);
            const before = meshes(lastScene()).find(mesh => mesh.renderOrder === 0);
            const release = vi.spyOn(required(before, "objects").geometry, "dispose");
            map.setSceneGeometry({ terrain: square(1), objects }, false);
            expect(meshes(lastScene()).find(mesh => mesh.renderOrder === 0)).toBe(before);
            expect(release).not.toHaveBeenCalled();
        });

        it("removes a surface that is no longer present", () => {
            map.setSceneGeometry({ terrain: square(0), objects: square(5) }, true);
            map.setSceneGeometry({ terrain: square(0), objects: null }, false);
            expect(meshes(lastScene())).toHaveLength(1);
            map.setSceneGeometry({ terrain: null, objects: null }, false);
            expect(meshes(lastScene())).toHaveLength(0);
        });

        it("patches only the named vertices in place", () => {
            const base = square(5);
            map.setSceneGeometry({ terrain: null, objects: base }, true);
            const mesh = required(meshes(lastScene())[0], "the mesh");
            const position = mesh.geometry.getAttribute("position") as THREE.BufferAttribute;
            const patched = { ...base, positions: [-1, 9, -1, 1, 5, -1, 0, 5, 1], patchOf: { base, ranges: [[0, 1]] as const } };
            map.setSceneGeometry({ terrain: null, objects: patched }, false);
            expect(meshes(lastScene())[0]).toBe(mesh);
            expect(position.getY(0)).toBe(9);
            expect(position.updateRanges).toEqual([{ start: 0, count: 3 }]);
        });

        it("rebuilds when a patch does not match what is drawn", () => {
            const base = square(5);
            map.setSceneGeometry({ terrain: null, objects: square(6) }, true);
            const mesh = meshes(lastScene())[0];
            map.setSceneGeometry({ terrain: null, objects: { ...base, patchOf: { base, ranges: [[0, 1]] as const } } }, false);
            expect(meshes(lastScene())[0]).not.toBe(mesh);
        });

        it("picks the nearest surface for objects and looks through them for the ground", () => {
            const wide = (y: number) => ({ positions: [-500, y, -500, 500, y, -500, 0, y, 500], indices: [0, 2, 1] });
            const lookDown = () => {
                lastScene();
                const camera = gpu.render.mock.lastCall![1] as THREE.PerspectiveCamera;
                camera.position.set(0, 200, 1);
                camera.lookAt(0, 0, 0);
            };
            map.setSceneGeometry({ terrain: wide(0), objects: wide(40) }, true);
            lookDown();
            expect(map.pickVertex(320, 180)).not.toBeNull();
            expect(map.pickSurface(320, 180)?.vertex).toBeDefined();
            map.setSceneGeometry({ terrain: wide(40), objects: wide(0) }, true);
            lookDown();
            expect(map.pickVertex(320, 180)).toBeNull();
        });

        it("fits the camera to both surfaces", () => {
            map.setSceneGeometry({ terrain: { positions: [-100, 0, -100, 100, 0, -100, 0, 0, 100], indices: [0, 1, 2] }, objects: square(0) }, true);
            map.captureImage();
            expect((gpu.render.mock.lastCall![1] as THREE.Camera).position.length()).toBeGreaterThan(100);
        });
    });

    it("draws, replaces and hides a stroke preview", () => {
        const tile = { positions: Array(18).fill(1), uvs: Array(12).fill(0), rgb: [0xffb800], textureId: null, opacity: 0.45 };
        scene.setStrokePreview(null);
        scene.setStrokePreview(tile);
        const preview = (scene as unknown as { strokePreview: THREE.Mesh<THREE.BufferGeometry, THREE.MeshBasicMaterial> }).strokePreview;
        expect(preview.visible).toBe(true);
        expect(preview.material.transparent).toBe(true);
        expect(preview.geometry.getAttribute("color").count).toBe(6);
        scene.setStrokePreview({ ...tile, positions: Array(36).fill(1), uvs: Array(24).fill(0), rgb: [1, 2], opacity: 1 });
        expect(preview.geometry.getAttribute("position").count).toBe(12);
        expect(preview.material.transparent).toBe(false);
        scene.setStrokePreview(null);
        expect(preview.visible).toBe(false);
    });

    it("uses a loaded texture for a stroke preview and releases the preview on dispose", () => {
        scene.setTextures(new Map([[8, { image: {} as ImageBitmap, alphaMode: 0 as const }]]));
        const tile = { positions: Array(18).fill(1), uvs: Array(12).fill(0), rgb: [0xffffff], textureId: 8, opacity: 1 };
        scene.setStrokePreview(tile);
        const preview = (scene as unknown as { strokePreview: THREE.Mesh<THREE.BufferGeometry, THREE.MeshBasicMaterial> }).strokePreview;
        expect(preview.material.map).not.toBeNull();
        scene.setStrokePreview({ ...tile, textureId: 9 });
        expect(preview.material.map).toBeNull();
        const release = vi.spyOn(preview.material, "dispose");
        scene.dispose();
        expect(release).toHaveBeenCalledOnce();
    });

    it("draws and hides the brush outline", () => {
        scene.setBrushOutline(null);
        scene.setBrushOutline([0, 0, 0, 512, 0, 0, 512, 0, 512]);
        const outline = (scene as unknown as { brushOutline: THREE.LineLoop }).brushOutline;
        expect(outline.visible).toBe(true);
        expect(outline.geometry.getAttribute("position").count).toBe(3);
        scene.setBrushOutline(null);
        expect(outline.visible).toBe(false);
    });

    it("captures a freshly drawn frame", () => {
        gpu.render.mockClear();
        scene.setGeometry(triangle);
        expect(scene.captureImage()).toBe("data:image/png;base64,frame");
        expect(gpu.render).toHaveBeenCalledOnce();
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

    it("keeps blend draws within the terrain-only weight buffer in a mixed mesh", () => {
        scene.setGeometry({
            positions: [...triangle.positions, ...triangle.positions],
            indices: [0, 1, 2, 3, 4, 5],
            textureIds: [-2, -1],
            terrainWeights: Array(3).fill([1, 0, 0, 0, 0, 0, 0, 0]).flat(),
            terrainBlends: [{ id: -2, layers: [{ textureId: 1, textureScale: 512, color: [1, 1, 1] }] }],
        });
        const { geometry } = renderAt(0);
        const weights = geometry.getAttribute("terrainWeightsA");
        expect(weights.count).toBe(3);
        expect(geometry.getAttribute("position").count).toBe(6);
        expect(geometry.groups).toEqual([{ start: 0, count: 3, materialIndex: 2 }, { start: 3, count: 3, materialIndex: 0 }]);
        expect(Array.from(geometry.index!.array)).toEqual([0, 1, 2, 3, 4, 5]);
    });

    it("keeps compact source indices separate from the permuted draw index", () => {
        const indices = Uint16Array.of(0, 1, 2, 3, 4, 5, 6, 7, 8);
        const image = {} as ImageBitmap;
        scene.setTextures(new Map([[7, { image, alphaMode: 0 }]]));
        scene.setGeometry({ positions: [...triangle.positions, ...triangle.positions, ...triangle.positions], indices, textureIds: [7, -1, 7] });
        const draw = renderAt(0).geometry.index!.array;
        expect(draw).toBeInstanceOf(Uint16Array);
        expect(draw).not.toBe(indices);
        expect(draw).toEqual(Uint16Array.of(0, 1, 2, 6, 7, 8, 3, 4, 5));
        expect(indices).toEqual(Uint16Array.of(0, 1, 2, 3, 4, 5, 6, 7, 8));
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
        const initial = shader.uniforms.cacheWaterParams.value.clone();
        renderAt(1000);
        expect(shader.uniforms.cacheWaterParams.value).not.toEqual(initial);
        expect(release).not.toHaveBeenCalled();
        images.set(8, { ...water, image: {} as ImageBitmap, alphaMode: 1 });
        scene.setTextures(new Map(images)); scene.setGeometry(model);
        const replacedMesh = renderAt(2000);
        const replacement = replacedMesh.material[replacedMesh.geometry.groups[0].materialIndex ?? 0];
        expect(replacement).not.toBe(material);
        expect(replacement.transparent).toBe(true);
        expect(replacement.alphaTest).toBe(0);
        expect(release).toHaveBeenCalledOnce();
        const previous = shader.uniforms.cacheWaterParams.value.clone();
        renderAt(3000);
        expect(shader.uniforms.cacheWaterParams.value).toEqual(previous);
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

    it("skips transparent sorting during a pan and reorders on camera reversal", () => {
        scene.setGeometry(layered);
        const mesh = renderAt(0);
        expect(faceOrder(mesh)).toEqual([0, 2, 1]);
        expect(mesh.geometry.groups).toEqual([{ start: 0, count: 9, materialIndex: 1 }]);
        const index = mesh.geometry.index!;
        const version = index.version;
        const rebuildGroups = vi.spyOn(mesh.geometry, "clearGroups");
        const camera = gpu.render.mock.lastCall![1] as THREE.PerspectiveCamera;
        camera.position.addScalar(1);
        renderAt(8);
        expect(rebuildGroups).not.toHaveBeenCalled();
        expect(index.version).toBe(version);
        camera.position.set(0, 0, -20); camera.lookAt(0, 0, 0);
        renderAt(16);
        expect(rebuildGroups).toHaveBeenCalledOnce();
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
        expect(shader.fragmentShader.indexOf("outgoingLight = diffuseColor.rgb;")).toBeLessThan(shader.fragmentShader.indexOf("cacheSpecularLobe"));
        expect(shader.fragmentShader).toContain("dot(normal, normalize(vec3(0.091, 0.899, -0.428)))");
        expect(shader.fragmentShader).not.toContain("cacheReflection");
    });

    it("adds the client's specular lobe for effect 1 instead of blending in a sky", () => {
        scene.setTextures(new Map([[8,{image:{} as ImageBitmap,alphaMode:0,effectId:1,effectParameter:2}]]));
        scene.setGeometry({...triangle,preparedLighting:true,colors:Array(9).fill(0.5),textureIds:[8]});
        const shader = {uniforms:{},vertexShader:THREE.ShaderLib.standard.vertexShader,fragmentShader:THREE.ShaderLib.standard.fragmentShader};
        renderAt(0).material[2].onBeforeCompile(shader as unknown as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);

        expect(shader.fragmentShader).not.toContain("cacheSky");
        expect(shader.fragmentShader).toContain("sampledDiffuseColor.a");
        for (const exponent of ["128.0", "24.0", "5.0"]) expect(shader.fragmentShader).toContain(exponent);
        expect((shader.uniforms as Record<string, {value: THREE.Vector4}>).cachePhase.value.w).toBe(2);
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
        renderAt(0); renderAt(16); renderAt(39);
        const rendered = gpu.render.mock.lastCall![0] as THREE.Scene;
        const points = rendered.children.find(child => child instanceof THREE.Points) as THREE.Points<THREE.BufferGeometry,THREE.ShaderMaterial>;
        expect(points.geometry.getAttribute("particleColor").getW(0)).toBe(0);
        renderAt(40);
        expect(points.geometry.getAttribute("particleColor").getW(0)).toBeGreaterThan(0);
        const releaseGeometry = vi.spyOn(points.geometry,"dispose"), releaseMaterial = vi.spyOn(points.material,"dispose");
        scene.setGeometry({...triangle,sourceId: 2}); renderAt(60);
        expect(rendered.children.some(child => child instanceof THREE.Points)).toBe(false);
        expect(releaseGeometry).toHaveBeenCalledTimes(1); expect(releaseMaterial).toHaveBeenCalledTimes(1);
        expect(gpu.setAnimationLoop).toHaveBeenCalledTimes(1);
    });

    it.each([1, 7])("binds deterministic shader parameters for effect %s and clears old updates", id => {
        scene.setTextures(new Map([[8, {image: {} as ImageBitmap, alphaMode: 0, effectId: id, effectParameter: 19, effectParameter2: 1}]]));
        scene.setGeometry({...triangle, textureIds: [8]});
        const material = renderAt(0).material[2];
        const shader = {uniforms: {}, vertexShader: THREE.ShaderLib.standard.vertexShader, fragmentShader: THREE.ShaderLib.standard.fragmentShader};
        material.onBeforeCompile(shader as unknown as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
        renderAt(2500);
        const uniforms = shader.uniforms as Record<string, {value: THREE.Vector4}>;
        expect(uniforms.cachePhase.value.x).toBe(0.625);
        expect(shader.fragmentShader).toContain("uniform vec4 cachePhase");
        if (id === 7) expect(shader.fragmentShader).toContain("cacheReflection");
        const previous = uniforms.cachePhase.value.clone();
        scene.setTextures(new Map()); renderAt(3000);
        expect(uniforms.cachePhase.value).toEqual(previous);
    });

    it.each([[2, [0.625, 64, 0.1]], [4, [0.625, 0, 0]], [5, [0.75, -0, 1 / 2048]]] as const)(
        "updates and retires fixed water effect %s through the same animation path", (id, expected) => {
            scene.setTextures(new Map([[8, { image: {} as ImageBitmap, alphaMode: 0, effectId: id, effectParameter: 19, effectParameter2: 1 }]]));
            scene.setGeometry({ ...triangle, textureIds: [8] });
            const material = renderAt(0).material[2];
            const shader = { uniforms: {} as Record<string, THREE.IUniform>, vertexShader: THREE.ShaderLib.basic.vertexShader, fragmentShader: THREE.ShaderLib.basic.fragmentShader };
            material.onBeforeCompile(shader as unknown as THREE.WebGLProgramParametersWithUniforms, {} as THREE.WebGLRenderer);
            renderAt(2500);
            const parameters = shader.uniforms.cacheWaterParams.value as THREE.Vector4;
            expect(parameters.toArray().slice(0, 3)).toEqual(expected);
            const previous = parameters.clone();
            scene.setTextures(new Map());
            renderAt(3000);
            expect(parameters).toEqual(previous);
        },
    );
});
