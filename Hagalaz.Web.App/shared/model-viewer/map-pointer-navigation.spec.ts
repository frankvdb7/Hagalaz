// @vitest-environment jsdom
import { PerspectiveCamera, Vector3 } from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";
import { describe, expect, it, vi } from "vitest";
import { MapPointerNavigation } from "./map-pointer-navigation";

function setup() {
    const camera = new PerspectiveCamera(40, 1, 1, 100_000);
    camera.position.set(42_000, 16_800, 5040);
    const canvas = document.createElement("canvas");
    Object.defineProperties(canvas, { clientWidth: { value: 1000 }, clientHeight: { value: 1000 } });
    canvas.getBoundingClientRect = () => new DOMRect(0, 0, 1000, 1000);
    let captured: number | undefined;
    canvas.setPointerCapture = vi.fn(id => { captured = id; });
    canvas.hasPointerCapture = id => captured === id;
    canvas.releasePointerCapture = vi.fn(() => { captured = undefined; });
    const controls = new OrbitControls(camera, canvas);
    controls.zoomToCursor = true;
    const navigation = new MapPointerNavigation(canvas, camera, controls, () => 0);
    return {
        camera, canvas, controls, navigation,
        project(point: Vector3) {
            camera.updateMatrixWorld();
            const { x, y } = point.clone().project(camera);
            return { x: (x + 1) * 500, y: (1 - y) * 500 };
        },
        wheel(x: number, y: number) {
            canvas.dispatchEvent(new WheelEvent("wheel", { cancelable: true, deltaY: -100, clientX: x, clientY: y }));
        },
        pointer(type: string, x: number, y: number, buttons = 2) {
            canvas.dispatchEvent(new PointerEvent(type, {
                pointerId: 1, isPrimary: true, pointerType: "mouse", button: 2, buttons, clientX: x, clientY: y,
            }));
        },
        dispose() {
            navigation.dispose();
            controls.dispose();
        },
    };
}

describe("map pointer navigation", () => {
    it.each([["front", 15_000], ["back", -15_000]] as const)("zooms proportionally around a %s tile", (_, tileX) => {
        const scene = setup();
        try {
            const tile = new Vector3(tileX, 0, 0);
            const before = scene.project(tile);
            const right = new Vector3().setFromMatrixColumn(scene.camera.matrixWorld, 0);
            const nextTile = tile.clone().addScaledVector(right, 512);
            const sizeBefore = scene.project(nextTile).x - before.x;
            scene.wheel(before.x, before.y);
            const after = scene.project(tile);
            expect(after.x).toBeCloseTo(before.x);
            expect(after.y).toBeCloseTo(before.y);
            expect((scene.project(nextTile).x - after.x) / sizeBefore).toBeCloseTo(1 / 0.95, 5);
        } finally { scene.dispose(); }
    });

    it.each([["front", 15_000], ["back", -15_000]] as const)("keeps a %s tile under the pointer during a right drag", (_, tileX) => {
        const scene = setup();
        try {
            const tile = new Vector3(tileX, 0, 0);
            const { x, y } = scene.project(tile);
            scene.pointer("pointerdown", x, y);
            scene.pointer("pointermove", x + 40, y + 20);
            expect(scene.project(tile).x).toBeCloseTo(x + 40);
            expect(scene.project(tile).y).toBeCloseTo(y + 20);
            scene.pointer("pointerup", x + 40, y + 20, 0);
            expect(scene.canvas.hasPointerCapture(1)).toBe(false);
            const after = scene.camera.position.clone();
            scene.pointer("pointermove", x + 80, y + 40);
            expect(scene.camera.position).toEqual(after);
        } finally { scene.dispose(); }
    });

    it("uses the normal wheel step over the sky and releases an active drag on disposal", () => {
        const scene = setup();
        try {
            scene.camera.position.set(42_000, 1000, 0);
            scene.controls.update();
            const radius = scene.camera.position.distanceTo(scene.controls.target);
            scene.wheel(500, 0);
            expect(scene.camera.position.distanceTo(scene.controls.target) / radius).toBeCloseTo(0.95, 5);
            scene.camera.position.set(42_000, 16_800, 5040);
            scene.controls.target.set(0, 0, 0);
            scene.controls.update();
            const ground = scene.project(new Vector3());
            scene.pointer("pointerdown", ground.x, ground.y);
            expect(scene.canvas.hasPointerCapture(1)).toBe(true);
            scene.navigation.dispose();
            expect(scene.canvas.hasPointerCapture(1)).toBe(false);
            const after = scene.camera.position.clone();
            scene.pointer("pointermove", ground.x + 40, ground.y);
            expect(scene.camera.position).toEqual(after);
        } finally { scene.dispose(); }
    });
});
