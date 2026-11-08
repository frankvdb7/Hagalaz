import * as THREE from "three";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { PlacementGizmo, type PlacementGizmoResult, type PlacementGizmoTarget, type PlacementGizmoView } from "./placement-gizmo";

interface FakeControls extends THREE.EventDispatcher<Record<string, { value?: boolean }>> {
    dragging: boolean;
    showX: boolean;
    showY: boolean;
    showZ: boolean;
    mode?: string;
    object?: THREE.Object3D;
    size: number;
    detach: () => void;
    dispose: () => void;
}

const latest = vi.hoisted(() => ({ controls: undefined as unknown }));

vi.mock("three/addons/controls/TransformControls.js", async () => {
    const { EventDispatcher, Object3D } = await import("three");
    return {
        TransformControls: class extends EventDispatcher {
            dragging = false;
            showX = true;
            showY = true;
            showZ = true;
            mode?: string;
            object?: THREE.Object3D;
            size = 1;
            private readonly helper = new Object3D();
            detach = vi.fn(() => { this.object = undefined; });
            dispose = vi.fn();
            constructor() {
                super();
                latest.controls = this;
            }
            setMode(mode: string) { this.mode = mode; }
            setSize(size: number) { this.size = size; }
            setRotationSnap() { /* The real control rounds before reporting. */ }
            setScaleSnap() { /* The real control rounds before reporting. */ }
            attach(object: THREE.Object3D) { this.object = object; }
            getHelper() { return this.helper; }
        },
    };
});

vi.mock("./placement-gizmo-style", () => ({ stylePlacementGizmo: vi.fn() }));

describe("PlacementGizmo", () => {
    // Vertices 0 and 1 are the placement, 2 is another one.
    const start = [0, 0, 0, 512, 960, 512, 9000, 9000, 9000];
    let positions: THREE.BufferAttribute;
    let view: PlacementGizmoView & { setOrbitEnabled: ReturnType<typeof vi.fn>; rangeMoved: ReturnType<typeof vi.fn> };
    let gizmo: PlacementGizmo;
    let released: ReturnType<typeof vi.fn<(result: PlacementGizmoResult) => void>>;
    const controls = () => latest.controls as FakeControls;
    const target = (overrides: Partial<PlacementGizmoTarget> = {}): PlacementGizmoTarget => ({ mode: "translate", range: { startVertex: 0, endVertex: 2 }, tile: { x: 10, y: 20, plane: 0 }, scale: 1, snap: 1, onRelease: released, ...overrides });
    const startDrag = () => {
        controls().dragging = true;
        controls().dispatchEvent({ type: "dragging-changed", value: true });
    };
    const update = (change: (proxy: THREE.Object3D) => void) => {
        change(controls().object!);
        controls().dispatchEvent({ type: "objectChange" });
    };
    const finishDrag = () => {
        controls().dragging = false;
        controls().dispatchEvent({ type: "dragging-changed", value: false });
    };
    const vertices = () => Array.from(positions.array);

    beforeEach(() => {
        positions = new THREE.BufferAttribute(new Float32Array(start), 3);
        released = vi.fn<(result: PlacementGizmoResult) => void>();
        view = { scene: new THREE.Scene(), camera: new THREE.PerspectiveCamera(40), element: { clientHeight: 970 } as HTMLElement, positions: () => positions, setOrbitEnabled: vi.fn(), rangeMoved: vi.fn(), redraw: vi.fn() };
        gizmo = new PlacementGizmo(view);
    });

    it.each([
        ["translate", 0, true, true, true],
        ["translate", 4, true, false, true],
        ["rotate", 0, false, true, false],
        ["scale", 0, true, true, true],
    ] as const)("offers the %s handles a placement on plane %i can use", (mode, plane, showX, showY, showZ) => {
        gizmo.show(target({ mode, tile: { x: 10, y: 20, plane } }));
        expect(controls()).toMatchObject({ mode, showX, showY, showZ });
    });

    it("sits on the placement's centre and leaves when nothing is selected", () => {
        gizmo.show(target());
        expect(controls().object!.position.toArray()).toEqual([256, 480, 256]);
        gizmo.show(null);
        expect(controls().object).toBeUndefined();
    });

    it("moves only the selected vertices by whole tiles while dragging and edits once on release", () => {
        gizmo.show(target());
        startDrag();
        expect(view.setOrbitEnabled).toHaveBeenLastCalledWith(false);
        update(proxy => { proxy.position.x += 700; proxy.position.z -= 1100; });
        expect(vertices()).toEqual([512, 0, -1024, 1024, 960, -512, 9000, 9000, 9000]);
        expect(positions.updateRanges).toEqual([{ start: 0, count: 6 }]);
        expect(view.rangeMoved).toHaveBeenCalledOnce();
        expect(controls().object!.position.toArray()).toEqual([768, 480, -768]);
        expect(released).not.toHaveBeenCalled();
        finishDrag();
        expect(view.setOrbitEnabled).toHaveBeenLastCalledWith(true);
        expect(released.mock.calls).toEqual([[{ x: 11, y: 18, plane: 0, turns: 0, scale: 1 }]]);
    });

    it("lands on the grid but keeps an axis that was not dragged", () => {
        gizmo.show(target({ tile: { x: 5, y: 7, plane: 0 }, snap: 4 }));
        startDrag();
        update(proxy => { proxy.position.x += 512; });
        finishDrag();
        expect(released).toHaveBeenCalledWith({ x: 8, y: 7, plane: 0, turns: 0, scale: 1 });
    });

    it("keeps a move inside the region and its planes", () => {
        gizmo.show(target({ tile: { x: 62, y: 1, plane: 2 } }));
        startDrag();
        update(proxy => { proxy.position.add(new THREE.Vector3(5120, 5000, -5120)); });
        finishDrag();
        expect(released).toHaveBeenCalledWith({ x: 63, y: 0, plane: 3, turns: 0, scale: 1 });
    });

    it("turns clockwise in quarter steps around the placement", () => {
        gizmo.show(target({ mode: "rotate" }));
        startDrag();
        update(proxy => proxy.rotation.set(0, Math.PI / 2, 0));
        const [firstX, , firstZ] = vertices();
        expect([firstX, firstZ].map(Math.round)).toEqual([0, 512]);
        finishDrag();
        expect(released).toHaveBeenCalledWith({ x: 10, y: 20, plane: 0, turns: 1, scale: 1 });
    });

    it("keeps a long placement's south-west corner through a quarter turn", () => {
        positions = new THREE.BufferAttribute(new Float32Array([0, 0, 0, 512, 960, 1536]), 3);
        gizmo.show(target({ mode: "rotate" }));
        startDrag();
        update(proxy => proxy.rotation.set(0, Math.PI / 2, 0));
        expect(vertices().map(Math.round)).toEqual([0, 0, 512, 1536, 960, 0]);
        expect(controls().object!.position.toArray().map(Math.round)).toEqual([768, 480, 256]);
        finishDrag();
        expect(released).toHaveBeenCalledWith({ x: 10, y: 20, plane: 0, turns: 1, scale: 1 });
    });

    it("scales the whole placement from any handle within the allowed range", () => {
        gizmo.show(target({ mode: "scale", scale: 2 }));
        startDrag();
        update(proxy => proxy.scale.set(2, 3, 2));
        expect(vertices().slice(0, 6)).toEqual([-128, 0, -128, 640, 1440, 640]);
        expect(controls().object!.scale.toArray()).toEqual([3, 3, 3]);
        update(proxy => proxy.scale.set(2, 2, 40));
        finishDrag();
        expect(released).toHaveBeenCalledWith({ x: 10, y: 20, plane: 0, turns: 0, scale: 4 });
    });

    it("makes no edit when a drag ends where it began", () => {
        gizmo.show(target());
        startDrag();
        update(proxy => { proxy.position.x += 100; });
        finishDrag();
        expect(released).not.toHaveBeenCalled();
        expect(vertices()).toEqual(start);
    });

    it("keeps the dragged target when the scene updates mid-drag", () => {
        gizmo.show(target());
        startDrag();
        gizmo.show(null);
        expect(controls().object).toBeDefined();
    });

    describe("size", () => {
        const fitAt = (distance: number) => {
            view.camera.position.set(256, 480, 256 + distance);
            gizmo.fitToCamera();
            return controls().size;
        };
        const longest = 97 / (0.11875 * 970);
        const smallest = 40 / (0.11875 * 970);

        it("matches the placement at a middle distance", () => {
            gizmo.show(target());
            const armLength = Math.hypot(512, 960, 512) / 2 * 1.25;
            const distance = armLength * 8 / (0.5 * 1.9 * Math.tan(20 * Math.PI / 180));
            expect(fitAt(distance)).toBeCloseTo(0.5, 2);
        });

        it("stops growing up close and stays grabbable far away", () => {
            gizmo.show(target());
            expect(fitAt(1000)).toBeCloseTo(longest, 3);
            expect(fitAt(200000)).toBeCloseTo(smallest, 3);
        });

        it("keeps the same on-screen minimum in a short viewport", () => {
            (view.element as { clientHeight: number }).clientHeight = 471;
            gizmo.show(target());
            expect(fitAt(200000)).toBeCloseTo(40 / (0.11875 * 471), 3);
        });

        it("reaches past a large placement", () => {
            positions = new THREE.BufferAttribute(new Float32Array([0, 0, 0, 3000, 0, 4000]), 3);
            gizmo.show(target());
            view.camera.position.set(1500, 0, 2000 + 200000);
            gizmo.fitToCamera();
            expect(controls().size).toBeCloseTo(smallest, 3);
            view.camera.position.set(1500, 0, 2000 + 12000);
            gizmo.fitToCamera();
            expect(controls().size).toBeCloseTo(longest, 2);
        });

        it("does nothing with no selection", () => {
            view.camera.position.set(0, 0, 1000);
            gizmo.fitToCamera();
            expect(controls().size).toBe(1);
        });
    });

    it("releases its controls", () => {
        gizmo.dispose();
        expect(controls().dispose).toHaveBeenCalledOnce();
        expect(view.scene.children).toEqual([]);
    });
});
