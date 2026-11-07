import { afterNextRender, Component, computed, effect, ElementRef, inject, input, model, type OnDestroy, output, signal, viewChild } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatMenuModule } from "@angular/material/menu";
import type { ModelScene, ModelTexture } from "../../../../../shared/model-viewer/model-scene";
import type { MapObjectRange, MapSceneGeometry } from "./map-scene-geometry";

export const mapObjectDragType = "application/x-hagalaz-map-object";

@Component({
    selector: "cache-map-viewport",
    imports: [MatButtonModule, MatIconModule, MatMenuModule],
    template: `
        <div class="map-toolbar" role="group" aria-label="Map editor controls">
            <ng-content select="[mapEditTools]" />
            <button mat-stroked-button type="button" [matMenuTriggerFor]="cameraMenu"><mat-icon>my_location</mat-icon>Camera</button>
            <button mat-stroked-button type="button" [matMenuTriggerFor]="lightingMenu">
                <mat-icon>light_mode</mat-icon>Lighting: {{ regionLightingActive() ? 'Region atmosphere' : 'Default' }}<mat-icon iconPositionEnd>expand_more</mat-icon>
            </button>
            <button mat-stroked-button type="button" [matMenuTriggerFor]="viewMenu"><mat-icon>view_in_ar</mat-icon>View: {{ wireframe() ? 'Wireframe' : 'Textured' }}<mat-icon iconPositionEnd>expand_more</mat-icon></button>
            <button mat-icon-button type="button" aria-label="Toggle fullscreen" [disabled]="!fullscreenAvailable" (click)="fullscreen()"><mat-icon>fullscreen</mat-icon></button>
        </div>
        <mat-menu #cameraMenu="matMenu">
            <button mat-menu-item type="button" [matMenuTriggerFor]="orbitMenu">Orbit</button>
            <button mat-menu-item type="button" [matMenuTriggerFor]="panMenu">Pan</button>
            <button mat-menu-item type="button" [matMenuTriggerFor]="zoomMenu">Zoom</button>
            <button mat-menu-item type="button" (click)="reset()">Focus map</button>
        </mat-menu>
        <mat-menu #orbitMenu="matMenu">
            <button mat-menu-item type="button" (click)="orbit(-0.2, 0)">Orbit left</button>
            <button mat-menu-item type="button" (click)="orbit(0.2, 0)">Orbit right</button>
            <button mat-menu-item type="button" (click)="orbit(0, -0.2)">Orbit up</button>
            <button mat-menu-item type="button" (click)="orbit(0, 0.2)">Orbit down</button>
        </mat-menu>
        <mat-menu #panMenu="matMenu">
            <button mat-menu-item type="button" (click)="pan(-1, 0)">Pan left</button>
            <button mat-menu-item type="button" (click)="pan(1, 0)">Pan right</button>
            <button mat-menu-item type="button" (click)="pan(0, 1)">Pan up</button>
            <button mat-menu-item type="button" (click)="pan(0, -1)">Pan down</button>
        </mat-menu>
        <mat-menu #zoomMenu="matMenu">
            <button mat-menu-item type="button" (click)="zoom(0.8)">Zoom in</button>
            <button mat-menu-item type="button" (click)="zoom(1.25)">Zoom out</button>
        </mat-menu>
        <mat-menu #lightingMenu="matMenu">
            <button mat-menu-item type="button" role="menuitemradio" [attr.aria-checked]="regionLightingActive()" [disabled]="!hasRegionAtmosphere()" (click)="useRegionAtmosphere.set(true)">Region atmosphere</button>
            <button mat-menu-item type="button" role="menuitemradio" [attr.aria-checked]="!regionLightingActive()" (click)="useRegionAtmosphere.set(false)">Default lighting</button>
        </mat-menu>
        <mat-menu #viewMenu="matMenu">
            <button mat-menu-item type="button" (click)="wireframe.set(false)">Textured</button>
            <button mat-menu-item type="button" (click)="wireframe.set(true)">Wireframe</button>
            <button mat-menu-item type="button" (click)="grid.set(!grid())">{{ grid() ? 'Hide grid' : 'Show grid' }}</button>
        </mat-menu>
        <div #viewport class="map-canvas" tabindex="0" role="img"
            aria-label="3D map viewport. Click an object to select it. Arrow keys orbit, Shift and arrows pan, plus and minus zoom, Home resets."
            (keydown)="key($event)" (pointerdown)="startPick($event)" (pointermove)="movePick($event)"
            (pointerup)="finishPick($event)" (pointercancel)="cancelPick()"
            (dragover)="allowObjectDrop($event)" (drop)="dropObject($event)"></div>
        <ng-content select="[mapSelectionStatus]" />
        @if (error()) { <p role="alert" class="p-3">{{ error() }}</p> }
    `,
    styles: `
        :host { display: block; position: relative; background: var(--mat-sys-surface); }
        .map-toolbar { position: relative; z-index: 1; display: flex; flex-wrap: wrap; align-items: center; gap: 6px; padding: 12px 12px 4px; }
        .map-toolbar button { color: var(--mat-sys-on-surface); }
        .map-canvas { height: clamp(340px, 58dvh, 720px); width: 100%; outline-offset: -2px; }
        :host(:fullscreen) { display: flex; flex-direction: column; }
        :host(:fullscreen) .map-canvas { flex: 1; height: auto; min-height: 0; }
        @media (width > 920px) {
            :host { display: flex; flex: 1 1 0; flex-direction: column; min-height: 0; }
            .map-toolbar { flex-shrink: 0; }
            .map-canvas { flex: 1 1 0; height: auto; min-height: 0; }
        }
    `,
    host: { class: "min-w-0" },
})
export class MapViewportComponent implements OnDestroy {
    readonly geometry = input<MapSceneGeometry | null>(null);
    readonly selectedObject = model<MapObjectRange | null>(null);
    readonly objectDrop = output<{ id: number; x: number; z: number }>();
    readonly textures = input<ReadonlyMap<number, ModelTexture>>(new Map());
    readonly selectionKey = input("");
    readonly hasRegionAtmosphere = input(false);
    readonly useRegionAtmosphere = model(true);
    readonly regionLightingActive = computed(() => this.hasRegionAtmosphere() && this.useRegionAtmosphere());
    readonly wireframe = signal(false);
    readonly grid = signal(true);
    readonly error = signal<string | null>(null);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    readonly fullscreenAvailable = this.host.nativeElement.ownerDocument.fullscreenEnabled;
    private readonly viewport = viewChild.required<ElementRef<HTMLElement>>("viewport");
    private readonly scene = signal<ModelScene | null>(null);
    private destroyed = false;
    private fittedSelection: string | null = null;
    private pickStart?: { id: number; x: number; y: number };

    constructor() {
        afterNextRender(async () => {
            try {
                const { ModelScene } = await import("../../../../../shared/model-viewer/model-scene");
                if (!this.destroyed) {
                    const scene = new ModelScene(this.viewport().nativeElement, "Interactive 3D map regions", "map");
                    this.scene.set(scene);
                    scene.showOrientationGizmo();
                }
            } catch { this.error.set("3D rendering is unavailable. Enable WebGL in a supported browser."); }
        });
        effect(() => this.scene()?.setDisplay(this.grid(), this.wireframe()));
        effect(() => {
            const scene = this.scene(), geometry = this.geometry(), key = this.selectionKey();
            if (!scene) return;
            scene.setTextures(this.textures());
            scene.setSceneGeometry(geometry ?? { positions: [], indices: [] }, geometry !== null && key !== this.fittedSelection);
            if (geometry) this.fittedSelection = key;
        });
        effect(() => {
            const selected = this.selectedObject();
            const range = selected && this.geometry()?.placements.find(candidate => candidate.placement === selected.placement);
            this.scene()?.showVertexBounds(range ?? null);
            if (selected && !range) this.selectedObject.set(null);
        });
    }
    startPick(event: PointerEvent): void {
        this.pickStart = event.isPrimary && event.button === 0 ? { id: event.pointerId, x: event.clientX, y: event.clientY } : undefined;
    }
    movePick(event: PointerEvent): void {
        if (this.pickStart?.id !== event.pointerId) return;
        // OrbitControls listens on the document. Hold movement until this gesture becomes a drag.
        if (this.isPickClick(event)) event.stopPropagation();
        else this.cancelPick();
    }
    cancelPick(): void { this.pickStart = undefined; }
    finishPick(event: PointerEvent): void {
        if (this.pickStart?.id !== event.pointerId) return;
        const click = this.isPickClick(event);
        this.cancelPick();
        if (!click) return;
        const vertex = this.scene()?.pickVertex(event.clientX, event.clientY);
        const range = vertex == null ? null : this.geometry()?.placements.find(candidate => vertex >= candidate.startVertex && vertex < candidate.endVertex);
        this.selectedObject.set(range ?? null);
    }
    private isPickClick(event: PointerEvent): boolean {
        const start = this.pickStart;
        return start !== undefined && Math.hypot(event.clientX - start.x, event.clientY - start.y) <= 5;
    }
    allowObjectDrop(event: DragEvent): void {
        if (!event.dataTransfer?.types.includes(mapObjectDragType)) return;
        event.preventDefault();
        event.dataTransfer.dropEffect = "copy";
    }
    dropObject(event: DragEvent): void {
        const id = event.dataTransfer?.getData(mapObjectDragType);
        if (!id || !/^\d+$/.test(id)) return;
        event.preventDefault();
        const geometry = this.geometry();
        if (!geometry) return;
        const terrainEnd = geometry.placements[0]?.startVertex ?? geometry.positions.length / 3;
        const hit = this.scene()?.pickSurface(event.clientX, event.clientY, terrainEnd);
        if (hit) this.objectDrop.emit({ id: Number(id), x: hit.x, z: hit.z });
    }
    orbit(x: number, y: number): void { this.scene()?.orbit(x, y); }
    pan(x: number, y: number): void { this.scene()?.pan(x, y); }
    zoom(factor: number): void { this.scene()?.zoom(factor); }
    reset(): void { this.scene()?.resetCamera(); }
    async fullscreen(): Promise<void> {
        const element = this.host.nativeElement;
        if (element.ownerDocument.fullscreenElement === element) await element.ownerDocument.exitFullscreen();
        else await element.requestFullscreen();
    }
    key(event: KeyboardEvent): void {
        const arrows: Record<string, [number, number]> = { ArrowLeft: [-1, 0], ArrowRight: [1, 0], ArrowUp: [0, -1], ArrowDown: [0, 1] };
        const direction = arrows[event.key];
        if (direction) { event.preventDefault(); if (event.shiftKey) this.pan(direction[0], -direction[1]); else this.orbit(direction[0] * 0.2, direction[1] * 0.2); }
        else if (event.key === "+" || event.key === "=") { event.preventDefault(); this.zoom(0.8); }
        else if (event.key === "-") { event.preventDefault(); this.zoom(1.25); }
        else if (event.key === "Home") { event.preventDefault(); this.reset(); }
        else if (event.key === "Escape") this.selectedObject.set(null);
    }
    ngOnDestroy(): void { this.destroyed = true; this.scene()?.dispose(); }
}
