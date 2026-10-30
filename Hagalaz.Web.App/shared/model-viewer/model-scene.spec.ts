import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import * as THREE from "three";
import { ModelScene, type ModelTexture } from "./model-scene";

const gpu = vi.hoisted(() => ({
    setSize: vi.fn(), setAnimationLoop: vi.fn(), dispose: vi.fn(), remove: vi.fn(),
    disconnect: vi.fn(), controlsDispose: vi.fn(),
}));

vi.mock("three", async importOriginal => {
    const actual = await importOriginal<typeof import("three")>();
    return { ...actual, WebGLRenderer: class {
        domElement = { setAttribute: vi.fn(), remove: gpu.remove };
        setPixelRatio = vi.fn(); setSize = gpu.setSize; setAnimationLoop = gpu.setAnimationLoop;
        render = vi.fn(); dispose = gpu.dispose;
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
});
