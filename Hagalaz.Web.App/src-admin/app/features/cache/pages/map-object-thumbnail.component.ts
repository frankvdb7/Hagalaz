import { afterNextRender, Component, DestroyRef, effect, type ElementRef, inject, input, signal, untracked, viewChild } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import type { MapObjectRange, MapSceneGeometry } from "./map-scene-geometry";

@Component({
    selector: "cache-map-object-thumbnail",
    imports: [MatIconModule],
    template: `
        <canvas #canvas width="96" height="64" [style.visibility]="painted() ? 'visible' : 'hidden'" aria-hidden="true"></canvas>
        @if (!painted()) { <mat-icon aria-hidden="true">view_in_ar</mat-icon> }
    `,
    styles: `
        :host { position: relative; display: grid; place-items: center; width: 96px; height: 64px; }
        canvas { display: block; }
        mat-icon { position: absolute; color: var(--mat-sys-on-surface-variant); }
    `,
})
export class MapObjectThumbnailComponent {
    readonly geometry = input.required<MapSceneGeometry>();
    readonly range = input.required<MapObjectRange>();
    readonly ready = input(false);
    /** Changes only when scene colours change (appearances, textures, lighting), not on placement edits. */
    readonly colourKey = input<unknown>();
    readonly painted = signal(false);
    private readonly visible = signal(false);
    private readonly canvas = viewChild.required<ElementRef<HTMLCanvasElement>>("canvas");
    private paintedPlacement?: MapObjectRange["placement"];
    private paintedColourKey?: unknown;

    constructor() {
        const destroy = inject(DestroyRef);
        afterNextRender(() => {
            const observer = new IntersectionObserver(([entry]) => this.visible.set(entry.isIntersecting));
            observer.observe(this.canvas().nativeElement);
            destroy.onDestroy(() => observer.disconnect());
        });
        effect(onCleanup => {
            const range = this.range();
            const colourKey = this.colourKey();
            if ((this.paintedPlacement === range.placement && this.paintedColourKey === colourKey) || !this.ready() || !this.visible()) return;
            const geometry = untracked(this.geometry);
            this.painted.set(false);
            const draw = () => {
                this.paintedPlacement = range.placement;
                this.paintedColourKey = colourKey;
                this.painted.set(paintMesh(this.canvas().nativeElement, geometry, range));
            };
            if (window.requestIdleCallback) {
                const task = window.requestIdleCallback(draw);
                onCleanup(() => window.cancelIdleCallback(task));
            } else {
                const frame = requestAnimationFrame(draw);
                onCleanup(() => cancelAnimationFrame(frame));
            }
        });
    }
}

function paintMesh(canvas: HTMLCanvasElement, geometry: MapSceneGeometry, { startVertex, endVertex }: MapObjectRange): boolean {
    const context = canvas.getContext("2d");
    if (!context) return false;
    context.clearRect(0, 0, canvas.width, canvas.height);
    // Bound work for unusually large meshes. Map vertices are already expanded into triangles.
    if (endVertex - startVertex > 36000) return false;
    const points = new Float32Array((endVertex - startVertex) * 3);
    let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
    for (let vertex = startVertex; vertex < endVertex; vertex++) {
        const source = vertex * 3, target = (vertex - startVertex) * 3;
        const x = geometry.positions[source], y = geometry.positions[source + 1], z = geometry.positions[source + 2];
        const forward = x * 0.5 + z * 0.866;
        const horizontal = x * 0.866 - z * 0.5;
        const vertical = forward * 0.42 - y * 0.91;
        points[target] = horizontal;
        points[target + 1] = vertical;
        points[target + 2] = forward * 0.91 + y * 0.42;
        minX = Math.min(minX, horizontal); maxX = Math.max(maxX, horizontal);
        minY = Math.min(minY, vertical); maxY = Math.max(maxY, vertical);
    }
    const scale = Math.min((canvas.width - 12) / Math.max(1, maxX - minX), (canvas.height - 8) / Math.max(1, maxY - minY));
    const left = (canvas.width - (minX + maxX) * scale) / 2;
    const top = (canvas.height - (minY + maxY) * scale) / 2;
    const faces: { offset: number; depth: number; opacity: number }[] = [];
    const rgba = geometry.vertexRgba;
    for (let vertex = startVertex; vertex + 2 < endVertex; vertex += 3) {
        const opacity = rgba ? (rgba[vertex * 4 + 3] + rgba[vertex * 4 + 7] + rgba[vertex * 4 + 11]) / 3
            : ((geometry.opacities?.[vertex] ?? 1) + (geometry.opacities?.[vertex + 1] ?? 1) + (geometry.opacities?.[vertex + 2] ?? 1)) / 3;
        if (opacity <= 0) continue;
        const offset = (vertex - startVertex) * 3;
        faces.push({ offset, opacity, depth: points[offset + 2] + points[offset + 5] + points[offset + 8] });
    }
    faces.sort((a, b) => a.depth - b.depth);
    for (const { offset, opacity } of faces) {
        const stride = rgba ? 4 : 3;
        const color = (startVertex + offset / 3) * stride;
        const red = channel(rgba ?? geometry.colors, color, stride);
        const green = channel(rgba ?? geometry.colors, color + 1, stride);
        const blue = channel(rgba ?? geometry.colors, color + 2, stride);
        context.fillStyle = `rgba(${red},${green},${blue},${opacity})`;
        context.beginPath();
        context.moveTo(left + points[offset] * scale, top + points[offset + 1] * scale);
        context.lineTo(left + points[offset + 3] * scale, top + points[offset + 4] * scale);
        context.lineTo(left + points[offset + 6] * scale, top + points[offset + 7] * scale);
        context.fill();
    }
    return faces.length > 0;
}

function channel(colors: ArrayLike<number> | undefined, index: number, stride: number): number {
    const linear = ((colors?.[index] ?? 1) + (colors?.[index + stride] ?? 1) + (colors?.[index + stride * 2] ?? 1)) / 3;
    const srgb = linear <= 0.0031308 ? linear * 12.92 : 1.055 * linear ** (1 / 2.4) - 0.055;
    return Math.round(255 * Math.min(1, Math.max(0, srgb)));
}
