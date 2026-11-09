import { afterNextRender, Component, computed, effect, ElementRef, inject, input, model, type OnDestroy, output, signal, viewChild } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatDividerModule } from "@angular/material/divider";
import { MatIconModule } from "@angular/material/icon";
import { type MatMenu, MatMenuModule } from "@angular/material/menu";
import { MatTooltipModule } from "@angular/material/tooltip";
import type { ModelScene, ModelTexture, StrokePreviewGeometry } from "../../../../../shared/model-viewer/model-scene";
import { type MapAtmosphere, mapFogRange } from "../services/map-render.models";
import type { PlacementGizmoMode, PlacementGizmoResult } from "../../../../../shared/model-viewer/placement-gizmo";
import type { MapObjectRange, MapObjectTransform, MapSceneGeometry } from "./map-scene-geometry";
import type { BrushPoint } from "./map-terrain-edit";

export const mapObjectDragType = "application/x-hagalaz-map-object";

@Component({
    selector: "cache-map-viewport",
    imports: [MatButtonModule, MatDividerModule, MatIconModule, MatMenuModule, MatTooltipModule],
    template: `
        <div class="map-toolbar" role="group" aria-label="Map editor controls">
            <ng-content select="[mapEditTools]" />
            <button mat-button type="button" class="view-trigger" [matMenuTriggerFor]="viewMenu"><mat-icon>visibility</mat-icon>View<mat-icon iconPositionEnd>expand_more</mat-icon></button>
            <span class="toolbar-gap"></span>
            <button mat-icon-button type="button" aria-label="More actions" matTooltip="More actions" [matMenuTriggerFor]="actionsMenu"><mat-icon>more_vert</mat-icon></button>
        </div>
        <mat-menu #actionsMenu="matMenu">
            <button mat-menu-item type="button" [disabled]="!canSave()" (click)="save.emit()"><mat-icon>save</mat-icon>Save changes</button>
            <button mat-menu-item type="button" [disabled]="!canUndo()" (click)="undo.emit()"><mat-icon>undo</mat-icon>Undo</button>
            <button mat-menu-item type="button" [disabled]="!canUndo()" (click)="discardEdits.emit()"><mat-icon>delete_sweep</mat-icon>Discard all edits</button>
            <button mat-menu-item type="button" [disabled]="!canRebuildWorldMap()" (click)="rebuildWorldMap.emit()"><mat-icon>map</mat-icon>Rebuild world map</button>
            <mat-divider />
            <button mat-menu-item type="button" [disabled]="!geometry()" (click)="saveImage()"><mat-icon>photo_camera</mat-icon>Save image</button>
            <button mat-menu-item type="button" [disabled]="!fullscreenAvailable" (click)="fullscreen()"><mat-icon>fullscreen</mat-icon>Toggle fullscreen</button>
        </mat-menu>
        <mat-menu #viewMenu="matMenu" class="map-view-menu">
            <button mat-menu-item type="button" [matMenuTriggerFor]="gridMenu"><mat-icon>grid_on</mat-icon>Grid ({{ snap() }})</button>
            <button mat-menu-item type="button" [matMenuTriggerFor]="lightingMenu"><mat-icon>light_mode</mat-icon>Lighting: {{ regionLightingActive() ? 'Region atmosphere' : 'Default' }}{{ fogActive() ? ' with fog' : '' }}</button>
            <button mat-menu-item type="button" [matMenuTriggerFor]="displayMenu"><mat-icon>texture</mat-icon>{{ wireframe() ? 'Wireframe' : 'Textured' }}</button>
            <button mat-menu-item type="button" role="menuitemcheckbox" [attr.aria-checked]="animateObjects()" (click)="animateObjects.set(!animateObjects())">Animate objects</button>
            @if (layersMenu(); as layers) { <button mat-menu-item type="button" [matMenuTriggerFor]="layers"><mat-icon>layers</mat-icon>Layers</button> }
            <button mat-menu-item type="button" [matMenuTriggerFor]="cameraMenu"><mat-icon>my_location</mat-icon>Camera</button>
        </mat-menu>
        <mat-menu #gridMenu="matMenu">
            @for (size of snapSizes; track size) { <button mat-menu-item type="button" role="menuitemradio" [attr.aria-checked]="snap() === size" (click)="snap.set(size)">Snap to grid ({{ size }})</button> }
            <mat-divider />
            <button mat-menu-item type="button" role="menuitemcheckbox" [attr.aria-checked]="grid()" (click)="grid.set(!grid())">Show grid</button>
        </mat-menu>
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
            <mat-divider />
            <button mat-menu-item type="button" role="menuitemcheckbox" [attr.aria-checked]="fogActive()" [disabled]="!regionLightingActive()" (click)="fog.set(!fog())">Client fog</button>
        </mat-menu>
        <mat-menu #displayMenu="matMenu">
            <button mat-menu-item type="button" role="menuitemradio" [attr.aria-checked]="!wireframe()" (click)="wireframe.set(false)">Textured</button>
            <button mat-menu-item type="button" role="menuitemradio" [attr.aria-checked]="wireframe()" (click)="wireframe.set(true)">Wireframe</button>
        </mat-menu>
        <div #viewport class="map-canvas" [class.is-brush]="brushActive()" tabindex="0" role="img"
            aria-label="3D map viewport. Click an object to select it. Arrow keys orbit, Shift and arrows pan, plus and minus zoom, Home resets."
            (keydown)="key($event)" (pointerdown)="startPick($event)" (pointermove)="movePick($event)"
            (pointerup)="finishPick($event)" (pointercancel)="cancelPick()" (pointerleave)="leaveBrush()"
            (dragover)="allowObjectDrop($event)" (dragleave)="leaveObjectDrop($event)" (drop)="dropObject($event)"></div>
        <ng-content select="[mapSelectionStatus]" />
        <ng-content select="[mapLoadStatus]" />
        @if (error()) { <p role="alert" class="p-3">{{ error() }}</p> }
    `,
    styles: `
        :host { display: block; position: relative; background: var(--mat-sys-surface); }
        .map-toolbar { position: relative; z-index: 1; display: flex; flex-wrap: wrap; align-items: center; gap: 6px; padding: 8px 12px; border-bottom: 1px solid var(--mat-sys-outline-variant); }
        .map-toolbar button { color: var(--mat-sys-on-surface); }
        .map-toolbar .view-trigger { height: 36px; border-radius: 8px; color: var(--mat-sys-primary); }
        .toolbar-gap { flex: 1 1 0; }
        .map-canvas.is-brush { cursor: crosshair; touch-action: none; }
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
    readonly objectTransform = input<MapObjectTransform | null>(null);
    readonly gizmoMode = input<PlacementGizmoMode | null>(null);
    readonly objectGizmo = output<PlacementGizmoResult>();
    readonly objectDrop = output<{ id: number; x: number; z: number }>();
    /** The ground under a dragged object, or null once the drag is off the terrain or over. */
    readonly objectDragHover = output<BrushPoint | null>();
    readonly dropOutline = input<readonly number[] | null>(null);
    readonly brushActive = input(false);
    readonly brushOutline = input<readonly number[] | null>(null);
    readonly brushHover = output<BrushPoint | null>();
    readonly brushStroke = output<readonly BrushPoint[]>();
    /** The stroke so far, or null when one was cancelled. */
    readonly brushPreview = output<readonly BrushPoint[] | null>();
    readonly strokePreview = input<StrokePreviewGeometry | null>(null);
    readonly canRebuildWorldMap = input(false);
    readonly rebuildWorldMap = output<void>();
    readonly canSave = input(false);
    readonly save = output<void>();
    readonly canUndo = input(false);
    readonly undo = output<void>();
    readonly discardEdits = output<void>();
    readonly textures = input<ReadonlyMap<number, ModelTexture>>(new Map());
    readonly selectionKey = input("");
    readonly regionAtmosphere = input<MapAtmosphere | null>(null);
    readonly environmentFaces = input<readonly ModelTexture[] | null>(null);
    readonly hasRegionAtmosphere = computed(() => this.regionAtmosphere() !== null);
    readonly useRegionAtmosphere = model(true);
    readonly regionLightingActive = computed(() => this.hasRegionAtmosphere() && this.useRegionAtmosphere());
    // Off by default: the fitted overview camera sits beyond the client's far plane.
    readonly fog = signal(false);
    readonly fogActive = computed(() => this.regionLightingActive() && this.fog());
    readonly environment = computed(() => {
        const atmosphere = this.regionLightingActive() ? this.regionAtmosphere() : null;
        return {
            skyRgb: atmosphere?.skyRgb ?? null,
            fog: atmosphere && this.fog() ? mapFogRange(atmosphere.fogValue) : null,
            environmentFaces: atmosphere ? this.environmentFaces() : null,
        };
    });
    readonly snap = model(1);
    /** Layer visibility belongs to the page, which supplies its own menu. */
    readonly layersMenu = input<MatMenu>();
    readonly snapSizes = [1, 2, 4];
    readonly wireframe = signal(false);
    readonly animateObjects = signal(!(globalThis.matchMedia?.("(prefers-reduced-motion: reduce)").matches ?? false));
    readonly grid = signal(true);
    readonly error = signal<string | null>(null);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    readonly fullscreenAvailable = this.host.nativeElement.ownerDocument.fullscreenEnabled;
    private readonly viewport = viewChild.required<ElementRef<HTMLElement>>("viewport");
    private readonly scene = signal<ModelScene | null>(null);
    private destroyed = false;
    private fittedSelection: string | null = null;
    private pickStart?: { id: number; x: number; y: number };
    private stroke?: { id: number; points: BrushPoint[] };
    private brushFrame?: number;
    private latestBrush?: { x: number; y: number };
    private dropFrame?: number;
    private latestDrop?: { x: number; y: number };
    private dropOnTerrain = true;

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
        effect(() => this.scene()?.setObjectAnimationsEnabled(this.animateObjects()));
        effect(() => this.scene()?.setPrimaryDragEnabled(!this.brushActive()));
        effect(() => this.scene()?.setBrushOutline(this.dropOutline() ?? (this.brushActive() ? this.brushOutline() : null)));
        effect(() => this.scene()?.setStrokePreview(this.brushActive() ? this.strokePreview() : null));
        effect(() => {
            const { skyRgb, fog, environmentFaces } = this.environment();
            this.scene()?.setEnvironment(skyRgb, fog, environmentFaces);
        });
        effect(() => {
            const scene = this.scene(), geometry = this.geometry(), key = this.selectionKey();
            if (!scene) return;
            scene.setTextures(this.textures());
            scene.setSceneGeometry(geometry ?? { terrain: null, objects: null }, geometry !== null && key !== this.fittedSelection);
            if (geometry) this.fittedSelection = key;
        });
        effect(() => {
            const selected = this.selectedObject();
            const range = selected && this.geometry()?.placements.find(candidate => candidate.placement === selected.placement);
            this.scene()?.showVertexBounds(range ?? null);
            const transform = this.objectTransform(), mode = this.gizmoMode();
            this.scene()?.showPlacementGizmo(range && transform && mode ? { mode, range, tile: transform, scale: transform.scale, snap: this.snap(), onRelease: result => this.objectGizmo.emit(result) } : null);
            if (selected && !range) this.selectedObject.set(null);
        });
    }
    startPick(event: PointerEvent): void {
        if (this.brushActive()) { this.startStroke(event); return; }
        // A press on the handles belongs to the gizmo, not to picking.
        this.pickStart = event.isPrimary && event.button === 0 && !this.scene()?.placementGizmoDragging ? { id: event.pointerId, x: event.clientX, y: event.clientY } : undefined;
    }
    movePick(event: PointerEvent): void {
        if (this.brushActive()) { this.queueBrushSample(event); return; }
        if (this.pickStart?.id !== event.pointerId) return;
        // OrbitControls listens on the document. Hold movement until this gesture becomes a drag.
        if (this.isPickClick(event)) event.stopPropagation();
        else this.cancelPick();
    }
    cancelPick(): void {
        this.pickStart = undefined;
        if (this.stroke) this.brushPreview.emit(null);
        this.stroke = undefined;
    }
    finishPick(event: PointerEvent): void {
        if (this.brushActive()) { this.finishStroke(event); return; }
        if (this.pickStart?.id !== event.pointerId) return;
        const click = this.isPickClick(event);
        this.cancelPick();
        if (!click) return;
        const vertex = this.scene()?.pickVertex(event.clientX, event.clientY);
        const range = vertex == null ? null : this.geometry()?.placements.find(candidate => vertex >= candidate.startVertex && vertex < candidate.endVertex);
        this.selectedObject.set(range ?? null);
    }
    private startStroke(event: PointerEvent): void {
        if (!event.isPrimary || event.button !== 0) return;
        this.stroke = { id: event.pointerId, points: [] };
        // Keeps the stroke's pointerup when the drag ends outside the viewport.
        this.viewport().nativeElement.setPointerCapture?.(event.pointerId);
        this.sampleBrush(event.clientX, event.clientY);
    }
    private queueBrushSample(event: PointerEvent): void {
        this.latestBrush = { x: event.clientX, y: event.clientY };
        // Ray casts against the whole scene, so sample once a frame.
        this.brushFrame ??= requestAnimationFrame(() => {
            this.brushFrame = undefined;
            if (this.latestBrush) this.sampleBrush(this.latestBrush.x, this.latestBrush.y);
        });
    }
    private sampleBrush(x: number, y: number): void {
        const point = this.terrainHit(x, y);
        this.brushHover.emit(point);
        if (!point || !this.stroke) return;
        this.stroke.points.push(point);
        this.brushPreview.emit([...this.stroke.points]);
    }
    private finishStroke(event: PointerEvent): void {
        if (this.stroke?.id !== event.pointerId) return;
        this.sampleBrush(event.clientX, event.clientY);
        const { points } = this.stroke;
        this.stroke = undefined;
        if (points.length) this.brushStroke.emit(points);
    }
    leaveBrush(): void {
        if (this.brushActive() && !this.stroke) this.brushHover.emit(null);
    }
    private terrainHit(clientX: number, clientY: number): BrushPoint | null {
        const hit = this.scene()?.pickSurface(clientX, clientY);
        return hit ? { x: hit.x, z: hit.z } : null;
    }
    private isPickClick(event: PointerEvent): boolean {
        const start = this.pickStart;
        return start !== undefined && Math.hypot(event.clientX - start.x, event.clientY - start.y) <= 5;
    }
    allowObjectDrop(event: DragEvent): void {
        if (!event.dataTransfer?.types.includes(mapObjectDragType)) return;
        event.preventDefault();
        event.dataTransfer.dropEffect = this.dropOnTerrain ? "copy" : "none";
        this.latestDrop = { x: event.clientX, y: event.clientY };
        // Ray casts against the whole scene, so sample once a frame.
        this.dropFrame ??= requestAnimationFrame(() => {
            this.dropFrame = undefined;
            if (!this.latestDrop) return;
            const hit = this.terrainHit(this.latestDrop.x, this.latestDrop.y);
            this.dropOnTerrain = hit !== null;
            this.objectDragHover.emit(hit);
        });
    }
    leaveObjectDrop(event: DragEvent): void {
        // Also fires when moving between the viewport's own canvases.
        if (!this.viewport().nativeElement.contains(event.relatedTarget as Node | null)) this.endObjectDrag();
    }
    private endObjectDrag(): void {
        if (this.dropFrame !== undefined) cancelAnimationFrame(this.dropFrame);
        this.dropFrame = this.latestDrop = undefined;
        this.dropOnTerrain = true;
        this.objectDragHover.emit(null);
    }
    dropObject(event: DragEvent): void {
        this.endObjectDrag();
        const id = event.dataTransfer?.getData(mapObjectDragType);
        if (!id || !/^\d+$/.test(id)) return;
        event.preventDefault();
        const hit = this.terrainHit(event.clientX, event.clientY);
        if (hit) this.objectDrop.emit({ id: Number(id), x: hit.x, z: hit.z });
    }
    orbit(x: number, y: number): void { this.scene()?.orbit(x, y); }
    pan(x: number, y: number): void { this.scene()?.pan(x, y); }
    zoom(factor: number): void { this.scene()?.zoom(factor); }
    reset(): void { this.scene()?.resetCamera(); }
    saveImage(): void {
        const image = this.scene()?.captureImage();
        if (!image) return;
        const link = this.host.nativeElement.ownerDocument.createElement("a");
        link.href = image;
        link.download = `region-${this.selectionKey().replaceAll(":", "-")}.png`;
        link.click();
    }
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
    ngOnDestroy(): void {
        this.destroyed = true;
        if (this.brushFrame !== undefined) cancelAnimationFrame(this.brushFrame);
        if (this.dropFrame !== undefined) cancelAnimationFrame(this.dropFrame);
        this.scene()?.dispose();
    }
}
