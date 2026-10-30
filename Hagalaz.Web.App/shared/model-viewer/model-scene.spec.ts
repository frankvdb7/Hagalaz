import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import * as THREE from "three";
import { ModelScene, type ModelTexture } from "./model-scene";

const gpu = vi.hoisted(() => ({
    setSize: vi.fn(), setAnimationLoop: vi.fn(), dispose: vi.fn(), remove: vi.fn(),
    disconnect: vi.fn(), controlsDispose: vi.fn(), render: vi.fn(),
}));

vi.mock("three", async importOriginal => {
    const actual = await importOriginal<typeof import("three")>();
    return { ...actual, WebGLRenderer: class {
        domElement = { setAttribute: vi.fn(), remove: gpu.remove };
        setPixelRatio = vi.fn(); setSize = gpu.setSize; setAnimationLoop = gpu.setAnimationLoop;
        render = gpu.render; dispose = gpu.dispose;
    } };
});
vi.mock("three/addons/controls/OrbitControls.js", () => ({ OrbitControls: class {
    target = new THREE.Vector3(); update = vi.fn(); dispose = gpu.controlsDispose;
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
        expect(releaseGeometry).toHaveBeenCalledTimes(1);
        scene.dispose(); scene.dispose();
        expect(gpu.setAnimationLoop).toHaveBeenLastCalledWith(null);
        expect(gpu.disconnect).toHaveBeenCalledTimes(1);
        expect(gpu.controlsDispose).toHaveBeenCalledTimes(1);
        expect(gpu.dispose).toHaveBeenCalledTimes(1);
        expect(gpu.remove).toHaveBeenCalledTimes(1);
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
