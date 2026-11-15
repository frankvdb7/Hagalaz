import * as THREE from "three";
import type { TransformControls } from "three/addons/controls/TransformControls.js";
import { axisColors } from "./orientation-gizmo";

// The same amber as the selection box.
const activeColor = "#ffb800";
const groundHandleColor = "#edf1fa";
const groundHandleOpacity = 0.3;
const ringRadius = 0.5;
const ringThickness = 0.012;
const axisPickerWidth = 0.5;

type HandleMode = "translate" | "rotate" | "scale";
type HandleSet = Record<HandleMode, THREE.Object3D>;
type HandleMesh = THREE.Mesh<THREE.BufferGeometry, THREE.MeshBasicMaterial>;

/** Reduces the stock handles to the moves a placement can make, in the editor's colours. */
export function stylePlacementGizmo(controls: TransformControls): void {
    controls.setColors(axisColors.x, axisColors.up, axisColors.north, activeColor);
    const { gizmo, picker } = controls.getHelper().children[0] as unknown as { gizmo: HandleSet; picker: HandleSet };
    for (const handles of [gizmo, picker]) {
        // Free and diagonal moves would change plane and tile together, and every scale handle is uniform.
        removeHandles(handles.translate, mesh => ["XYZ", "XY", "YZ"].includes(mesh.name) || pointsBackward(mesh));
        removeHandles(handles.scale, mesh => ["XY", "YZ", "XZ"].includes(mesh.name) || pointsBackward(mesh));
    }
    for (const mesh of meshes(gizmo.translate)) if (mesh.name === "XZ") mesh.material = groundHandleMaterial(mesh.material);
    for (const mesh of meshes(picker.translate)) narrowAxisPicker(mesh);
    closeRotationRing(gizmo.rotate);
}

function removeHandles(handles: THREE.Object3D, unwanted: (mesh: HandleMesh) => boolean): void {
    for (const mesh of meshes(handles).filter(unwanted)) {
        mesh.removeFromParent();
        mesh.geometry.dispose();
    }
}

function pointsBackward(mesh: HandleMesh): boolean {
    const axis = ["X", "Y", "Z"].indexOf(mesh.name);
    return axis >= 0 && new THREE.Box3().setFromBufferAttribute(mesh.geometry.getAttribute("position") as THREE.BufferAttribute).getCenter(new THREE.Vector3()).getComponent(axis) < 0;
}

/** The stock grab cones are wide enough to cover the ground handle from a low camera. */
function narrowAxisPicker(mesh: HandleMesh): void {
    const axis = ["X", "Y", "Z"].indexOf(mesh.name);
    if (axis >= 0) mesh.geometry.scale(...[0, 1, 2].map(component => component === axis ? 1 : axisPickerWidth) as [number, number, number]);
}

function groundHandleMaterial(source: THREE.MeshBasicMaterial): THREE.MeshBasicMaterial {
    const material = source.clone();
    material.color.set(groundHandleColor);
    material.opacity = groundHandleOpacity;
    return material;
}

/** The stock ring is a half arc that faces the camera; a placement only turns one way, so show all of it. */
function closeRotationRing(handles: THREE.Object3D): void {
    for (const mesh of meshes(handles)) {
        if (mesh.name !== "Y") continue;
        mesh.geometry.dispose();
        mesh.geometry = new THREE.TorusGeometry(ringRadius, ringThickness, 8, 96).rotateX(Math.PI / 2);
    }
}

function meshes(handles: THREE.Object3D): HandleMesh[] {
    return [...handles.children] as HandleMesh[];
}
