import * as THREE from "three";
import { afterEach, describe, expect, it, vi } from "vitest";
import { OrientationGizmo } from "./orientation-gizmo";

describe("OrientationGizmo", () => {
    afterEach(() => vi.unstubAllGlobals());

    it("names the axes as map coordinates, with Z up and Y along the ground", () => {
        vi.stubGlobal("document", {
            createElement: () => {
                const canvas = { label: "", getContext: () => ({ strokeText: vi.fn(), fillText: (text: string) => (canvas.label = text) }) };
                return canvas;
            },
        });
        const gizmo = new OrientationGizmo(new THREE.PerspectiveCamera());
        const render = vi.fn();
        gizmo.render({ getViewport: (target: THREE.Vector4) => target.set(0, 0, 640, 360), clearDepth: vi.fn(), setViewport: vi.fn(), render } as unknown as THREE.WebGLRenderer);

        const root = render.mock.lastCall![0] as THREE.Group;
        const sprites = root.children.filter((child): child is THREE.Sprite => child instanceof THREE.Sprite);
        const directions = Object.fromEntries(sprites.map(sprite => [(sprite.material.map!.image as { label: string }).label, sprite.position.clone().normalize().toArray()]));

        expect(directions).toEqual({ X: [1, 0, 0], Z: [0, 1, 0], Y: [0, 0, 1] });
        gizmo.dispose();
    });
});
