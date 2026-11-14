import { Raycaster, Vector2, Vector3, type PerspectiveCamera } from "three";
import type { OrbitControls } from "three/addons/controls/OrbitControls.js";

/** Map mouse input uses a ground reference plane without raycasting the scene. */
export class MapPointerNavigation {
    private readonly listeners = new AbortController();
    private readonly raycaster = new Raycaster();
    private readonly pointer = new Vector2();
    private readonly forward = new Vector3();
    private drag?: { id: number; x: number; y: number; scale: number };

    constructor(
        private readonly canvas: HTMLCanvasElement,
        private readonly camera: PerspectiveCamera,
        private readonly controls: OrbitControls,
        private readonly groundHeight: () => number | undefined,
    ) {
        const options = { signal: this.listeners.signal, capture: true };
        canvas.addEventListener("wheel", this.wheel, { ...options, passive: true });
        canvas.addEventListener("pointerdown", this.startPan, options);
        canvas.addEventListener("pointermove", this.movePan, options);
        canvas.addEventListener("pointerup", this.endPan, options);
        canvas.addEventListener("pointercancel", this.endPan, options);
        canvas.addEventListener("lostpointercapture", this.endPan, options);
    }

    dispose(): void {
        this.listeners.abort();
        this.releasePan();
    }

    private distanceToGround(clientX: number, clientY: number): number | undefined {
        const height = this.groundHeight();
        const { left, top, width, height: canvasHeight } = this.canvas.getBoundingClientRect();
        if (height === undefined || !width || !canvasHeight) return undefined;
        this.camera.updateMatrixWorld();
        this.pointer.set(2 * (clientX - left) / width - 1, 1 - 2 * (clientY - top) / canvasHeight);
        this.raycaster.setFromCamera(this.pointer, this.camera);
        const { origin, direction } = this.raycaster.ray;
        if (Math.abs(direction.y) < 0.05) return undefined;
        const distance = (height - origin.y) / direction.y;
        return distance > this.camera.near && distance < this.camera.far ? distance : undefined;
    }

    private readonly wheel = (event: WheelEvent): void => {
        const { controls, camera } = this;
        if (!controls.enabled || !controls.enableZoom || this.drag || event.buttons || !event.deltaY) return;
        const distance = this.distanceToGround(event.clientX, event.clientY);
        if (distance === undefined) return;
        // OrbitControls applies its normal wheel step to this radius along the pointer ray.
        camera.getWorldDirection(controls.target).multiplyScalar(distance).add(camera.position);
    };

    /** Right-button pan on the primary pointer without modifiers, while panning is enabled and no pan is active. */
    private canStartPan(event: PointerEvent): boolean {
        const { enabled, enablePan } = this.controls;
        const plainRightButton = event.isPrimary && event.button === 2 && !(event.ctrlKey || event.metaKey || event.shiftKey);
        return enabled && enablePan && !this.drag && plainRightButton;
    }

    private readonly startPan = (event: PointerEvent): void => {
        const { controls, camera } = this;
        if (!this.canStartPan(event)) return;
        const distance = this.distanceToGround(event.clientX, event.clientY);
        const radius = camera.position.distanceTo(controls.target);
        if (distance === undefined || radius <= camera.near) return;
        const depth = distance * this.raycaster.ray.direction.dot(camera.getWorldDirection(this.forward));
        this.canvas.setPointerCapture(event.pointerId);
        this.drag = { id: event.pointerId, x: event.clientX, y: event.clientY, scale: depth / radius };
        event.stopImmediatePropagation();
    };

    private readonly movePan = (event: PointerEvent): void => {
        const drag = this.drag;
        if (drag?.id !== event.pointerId) return;
        if (!(event.buttons & 2) || !this.controls.enabled) {
            this.releasePan();
            return;
        }
        event.stopPropagation();
        this.controls.pan((event.clientX - drag.x) * drag.scale, (event.clientY - drag.y) * drag.scale);
        drag.x = event.clientX;
        drag.y = event.clientY;
    };

    private readonly endPan = (event: PointerEvent): void => {
        if (this.drag?.id === event.pointerId) this.releasePan();
    };

    private releasePan(): void {
        const drag = this.drag;
        this.drag = undefined;
        if (drag && this.canvas.hasPointerCapture(drag.id)) this.canvas.releasePointerCapture(drag.id);
    }
}
