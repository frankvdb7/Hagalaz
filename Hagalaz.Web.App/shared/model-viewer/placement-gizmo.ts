import * as THREE from "three";
import { TransformControls } from "three/addons/controls/TransformControls.js";
import { stylePlacementGizmo } from "./placement-gizmo-style";

const tileUnits = 512;
const planeUnits = 960;
const maxTile = 63;
const maxPlane = 3;
const quarterTurn = Math.PI / 2;
const scaleStep = 0.25;
const minScale = 0.25;
const maxScale = 4;
const minArmLength = 640;
const armOvershoot = 1.25;
const minArmPixels = 40;
const maxArmHeightShare = 0.1;
/** On-screen arm length per unit of TransformControls size, as a share of the viewport's height. */
const armHeightSharePerSize = 0.11875;

export type PlacementGizmoMode = "translate" | "rotate" | "scale";

export interface PlacementGizmoResult {
    x: number;
    y: number;
    plane: number;
    /** Clockwise quarter turns to add to the placement's rotation. */
    turns: number;
    scale: number;
}

export interface PlacementGizmoTarget {
    mode: PlacementGizmoMode;
    range: { startVertex: number; endVertex: number };
    tile: { x: number; y: number; plane: number };
    scale: number;
    /** Tiles per grid step. */
    snap: number;
    onRelease: (result: PlacementGizmoResult) => void;
}

/** What the gizmo needs from the scene that draws it. */
export interface PlacementGizmoView {
    scene: THREE.Scene;
    camera: THREE.PerspectiveCamera;
    element: HTMLElement;
    positions: () => THREE.BufferAttribute | undefined;
    setOrbitEnabled: (enabled: boolean) => void;
    rangeMoved: (range: PlacementGizmoTarget["range"]) => void;
    redraw: () => void;
}

/** Handles over the selected placement. A drag previews by moving its vertices; the edit happens on release. */
export class PlacementGizmo {
    private readonly proxy = new THREE.Object3D();
    private readonly controls: TransformControls;
    private readonly origin = new THREE.Vector3();
    private readonly pivot = new THREE.Vector3();
    private quarterTurnShift = 0;
    private armLength = minArmLength;
    private target: PlacementGizmoTarget | null = null;
    private storedPositions?: Float32Array;

    constructor(private readonly view: PlacementGizmoView) {
        this.controls = new TransformControls(view.camera, view.element);
        stylePlacementGizmo(this.controls);
        this.controls.setRotationSnap(quarterTurn);
        this.controls.setScaleSnap(scaleStep);
        this.controls.addEventListener("change", view.redraw);
        this.controls.addEventListener("dragging-changed", ({ value }) => this.dragChanged(value === true));
        this.controls.addEventListener("objectChange", () => this.previewDrag());
        view.scene.add(this.proxy, this.controls.getHelper());
    }

    get dragging(): boolean {
        return this.controls.dragging;
    }

    get helper(): THREE.Object3D {
        return this.controls.getHelper();
    }

    show(target: PlacementGizmoTarget | null): void {
        if (this.controls.dragging) return;
        const positions = target ? this.view.positions() : undefined;
        if (!target || !positions) {
            this.target = null;
            this.controls.detach();
            return;
        }
        this.target = target;
        const bounds = rangeBounds(positions, target.range);
        bounds.getCenter(this.origin);
        this.pivot.set(this.origin.x, bounds.min.y, this.origin.z);
        this.quarterTurnShift = ((bounds.max.z - bounds.min.z) - (bounds.max.x - bounds.min.x)) / 2;
        this.proxy.position.copy(this.origin);
        this.proxy.quaternion.identity();
        this.proxy.scale.setScalar(target.scale);
        this.controls.setMode(target.mode);
        this.controls.showX = this.controls.showZ = target.mode !== "rotate";
        // Underwater placements sit outside the planes a drag can reach.
        this.controls.showY = target.mode !== "translate" || target.tile.plane <= maxPlane;
        this.armLength = Math.max(minArmLength, bounds.getSize(new THREE.Vector3()).length() / 2 * armOvershoot);
        this.controls.attach(this.proxy);
        this.fitToCamera();
    }

    /** Keeps the handles in proportion to the placement as the camera moves, while staying large enough to grab. */
    fitToCamera(): void {
        if (!this.target) return;
        const { camera, element } = this.view;
        const unitsPerSize = camera.position.distanceTo(this.proxy.position) * Math.min(1.9 * Math.tan(camera.fov * Math.PI / 360) / camera.zoom, 7) / 8;
        const pixelsPerSize = armHeightSharePerSize * element.clientHeight;
        const size = clamp(this.armLength / unitsPerSize, minArmPixels / pixelsPerSize, Math.max(minArmPixels, maxArmHeightShare * element.clientHeight) / pixelsPerSize);
        if (Math.abs(size - this.controls.size) > 0.001) this.controls.setSize(size);
    }

    dispose(): void {
        this.view.scene.remove(this.proxy, this.controls.getHelper());
        this.controls.dispose();
    }

    private dragChanged(dragging: boolean): void {
        this.view.setOrbitEnabled(!dragging);
        const target = this.target;
        if (dragging) {
            const positions = this.view.positions();
            this.storedPositions = target && positions ? Float32Array.from(positions.array.slice(target.range.startVertex * 3, target.range.endVertex * 3)) : undefined;
            return;
        }
        this.storedPositions = undefined;
        if (!target) return;
        const result = this.resolve(target);
        if (isChange(target, result)) target.onRelease(result);
    }

    private previewDrag(): void {
        const target = this.target, positions = this.view.positions(), stored = this.storedPositions;
        if (!target || !positions || !stored) return;
        const result = this.resolve(target);
        // Handles step with the placement instead of following the pointer between tiles.
        this.proxy.position.copy(this.origin).add(this.previewOffset(target, result));
        this.proxy.scale.setScalar(result.scale);
        const matrix = this.previewMatrix(target, result);
        const point = new THREE.Vector3();
        const { startVertex, endVertex } = target.range;
        for (let vertex = startVertex; vertex < endVertex; vertex++) {
            point.fromArray(stored, (vertex - startVertex) * 3).applyMatrix4(matrix);
            positions.setXYZ(vertex, point.x, point.y, point.z);
        }
        positions.clearUpdateRanges();
        positions.addUpdateRange(startVertex * 3, (endVertex - startVertex) * 3);
        positions.needsUpdate = true;
        this.view.rangeMoved(target.range);
    }

    private resolve(target: PlacementGizmoTarget): PlacementGizmoResult {
        // Only move handles shift the placement; the proxy also follows a rotated footprint.
        const moved = target.mode === "translate" ? this.proxy.position.clone().sub(this.origin) : new THREE.Vector3();
        const { tile, snap } = target;
        const { x: scaleX, y: scaleY, z: scaleZ } = this.proxy.scale;
        // Every handle scales the whole placement, so take the axis being dragged.
        const dragged = [scaleX, scaleY, scaleZ].reduce((far, value) => Math.abs(value - target.scale) > Math.abs(far - target.scale) ? value : far);
        return {
            x: steppedTile(tile.x, moved.x, snap),
            y: steppedTile(tile.y, moved.z, snap),
            plane: clamp(Math.round(tile.plane + moved.y / planeUnits), 0, Math.max(maxPlane, tile.plane)),
            turns: quarterTurns(this.proxy.quaternion),
            scale: clamp(Math.round(dragged / scaleStep) * scaleStep, minScale, maxScale),
        };
    }

    private previewOffset({ tile }: PlacementGizmoTarget, result: PlacementGizmoResult): THREE.Vector3 {
        // A placement's footprint stays anchored at its south-west corner, so a quarter turn does not pivot on the centre.
        const anchored = result.turns % 2 === 1 ? this.quarterTurnShift : 0;
        return new THREE.Vector3((result.x - tile.x) * tileUnits + anchored, (result.plane - tile.plane) * planeUnits, (result.y - tile.y) * tileUnits - anchored);
    }

    private previewMatrix(target: PlacementGizmoTarget, result: PlacementGizmoResult): THREE.Matrix4 {
        const around = new THREE.Matrix4().makeTranslation(this.pivot);
        const back = new THREE.Matrix4().makeTranslation(this.pivot.clone().negate());
        return new THREE.Matrix4()
            .makeTranslation(this.previewOffset(target, result))
            .multiply(around)
            .multiply(new THREE.Matrix4().makeRotationY(result.turns * quarterTurn))
            .multiply(new THREE.Matrix4().makeScale(...uniform(result.scale / target.scale)))
            .multiply(back);
    }
}

function rangeBounds(positions: THREE.BufferAttribute, range: PlacementGizmoTarget["range"]): THREE.Box3 {
    const bounds = new THREE.Box3();
    const point = new THREE.Vector3();
    for (let vertex = range.startVertex; vertex < range.endVertex; vertex++) bounds.expandByPoint(point.fromBufferAttribute(positions, vertex));
    return bounds;
}

/** Lands on the grid, as typed coordinates do. An axis that was not dragged stays where it is. */
function steppedTile(tile: number, moved: number, snap: number): number {
    return moved === 0 ? tile : clamp(Math.round((tile + moved / tileUnits) / snap) * snap, 0, maxTile);
}

function quarterTurns(rotation: THREE.Quaternion): number {
    const turns = Math.round(2 * Math.atan2(rotation.y, rotation.w) / quarterTurn);
    return ((turns % 4) + 4) % 4;
}

function isChange({ tile, scale }: PlacementGizmoTarget, result: PlacementGizmoResult): boolean {
    return result.x !== tile.x || result.y !== tile.y || result.plane !== tile.plane || result.turns !== 0 || result.scale !== scale;
}

function uniform(value: number): [number, number, number] {
    return [value, value, value];
}

function clamp(value: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, value));
}
