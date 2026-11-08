import * as THREE from "three";
import { TransformControls } from "three/addons/controls/TransformControls.js";
import { describe, expect, it, vi } from "vitest";
import { axisColors } from "./orientation-gizmo";
import { stylePlacementGizmo } from "./placement-gizmo-style";

type Mode = "translate" | "rotate" | "scale";
type Handles = Record<"gizmo" | "picker", Record<Mode, THREE.Object3D>> & { materialLib: Record<string, THREE.MeshBasicMaterial> };

function styled() {
    const element = { addEventListener: vi.fn(), removeEventListener: vi.fn(), style: {} };
    const controls = new TransformControls(new THREE.PerspectiveCamera(), element as unknown as HTMLElement);
    stylePlacementGizmo(controls);
    return { controls, handles: controls.getHelper().children[0] as unknown as Handles };
}

const names = (group: THREE.Object3D) => [...new Set(group.children.map(child => child.name))].sort();
const extent = (mesh: THREE.Object3D) => new THREE.Box3().setFromBufferAttribute((mesh as THREE.Mesh).geometry.getAttribute("position") as THREE.BufferAttribute);

describe("stylePlacementGizmo", () => {
    it("keeps only the handles a placement can use", () => {
        const { handles } = styled();
        for (const set of [handles.gizmo, handles.picker]) {
            expect(names(set.translate)).toEqual(["X", "XZ", "Y", "Z"]);
            expect(names(set.scale)).toEqual(["X", "XYZ", "Y", "Z"]);
        }
    });

    it.each(["translate", "scale"] as const)("points the %s handles one way, so grabbing behind the object does nothing", mode => {
        const { handles } = styled();
        for (const set of [handles.gizmo, handles.picker]) {
            for (const mesh of set[mode].children) {
                const axis = ["X", "Y", "Z"].indexOf(mesh.name);
                if (axis >= 0) expect(extent(mesh).getCenter(new THREE.Vector3()).getComponent(axis)).toBeGreaterThan(0);
            }
        }
    });

    it("narrows the move arrows' grab areas so they leave the ground handle free", () => {
        const picker = styled().handles.picker.translate.children.find(child => child.name === "X")!;
        const bounds = extent(picker);
        expect(bounds.max.x).toBeCloseTo(0.6);
        expect(bounds.max.y).toBeLessThan(0.11);
    });

    it("uses the orientation gizmo's axis colours and the selection colour when active", () => {
        const { materialLib } = styled().handles;
        expect([materialLib["xAxis"], materialLib["yAxis"], materialLib["zAxis"], materialLib["active"]].map(material => `#${material.color.getHexString()}`)).toEqual([axisColors.x, axisColors.up, axisColors.north, "#ffb800"]);
    });

    it("gives the ground handle its own quiet colour without changing other handles", () => {
        const { handles } = styled();
        const ground = handles.gizmo.translate.children.find(child => child.name === "XZ") as THREE.Mesh<THREE.BufferGeometry, THREE.MeshBasicMaterial>;
        expect(ground.material.opacity).toBe(0.3);
        expect(ground.material).not.toBe(handles.materialLib["yAxisTransparent"]);
    });

    it("draws the rotation ring as a full circle around the vertical axis", () => {
        const ring = styled().handles.gizmo.rotate.children.find(child => child.name === "Y")!;
        const bounds = extent(ring);
        expect([bounds.min.x, bounds.max.x, bounds.min.z, bounds.max.z].map(value => Math.round(value * 10) / 10)).toEqual([-0.5, 0.5, -0.5, 0.5]);
        expect(bounds.max.y).toBeLessThan(0.02);
    });
});
