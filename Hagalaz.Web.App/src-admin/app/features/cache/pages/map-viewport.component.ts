import { afterNextRender, Component, effect, type ElementRef, input, type OnDestroy, signal, viewChild } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatMenuModule } from "@angular/material/menu";
import type { ModelGeometry, ModelScene, ModelTexture } from "../../../../../shared/model-viewer/model-scene";

@Component({
    selector: "cache-map-viewport",
    imports: [MatButtonModule, MatIconModule, MatMenuModule],
    template: `
        <div class="map-toolbar" role="group" aria-label="Map camera controls">
            <button mat-stroked-button type="button" [matMenuTriggerFor]="orbitMenu"><mat-icon>my_location</mat-icon>Orbit</button>
            <button mat-button type="button" [matMenuTriggerFor]="panMenu"><mat-icon>pan_tool</mat-icon>Pan</button>
            <button mat-button type="button" [matMenuTriggerFor]="zoomMenu"><mat-icon>search</mat-icon>Zoom</button>
            <button mat-button type="button" (click)="reset()"><mat-icon>center_focus_strong</mat-icon>Focus</button>
            <button mat-stroked-button type="button" [matMenuTriggerFor]="viewMenu"><mat-icon>view_in_ar</mat-icon>View: {{ wireframe() ? 'Wireframe' : 'Textured' }}</button>
        </div>
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
        <mat-menu #viewMenu="matMenu">
            <button mat-menu-item type="button" (click)="wireframe.set(false)">Textured</button>
            <button mat-menu-item type="button" (click)="wireframe.set(true)">Wireframe</button>
            <button mat-menu-item type="button" (click)="grid.set(!grid())">{{ grid() ? 'Hide grid' : 'Show grid' }}</button>
        </mat-menu>
        <div #viewport class="map-canvas" tabindex="0" role="img" aria-label="3D map viewport. Arrow keys orbit, Shift and arrows pan, plus and minus zoom, Home resets." (keydown)="key($event)"></div>
        @if (error()) { <p role="alert" class="p-3">{{ error() }}</p> }
    `,
    styles: `
        .map-toolbar { position: relative; z-index: 1; display: flex; flex-wrap: wrap; align-items: center; gap: 4px 8px; padding: 10px 16px 0; }
        .map-toolbar button { color: var(--mat-sys-on-surface); }
        .map-toolbar button:first-child { color: var(--mat-sys-primary); border-color: currentColor; }
        .map-canvas { height: clamp(340px, 58vh, 720px); width: 100%; outline-offset: -2px; }
    `,
    host: { class: "block min-w-0" },
})
export class MapViewportComponent implements OnDestroy {
    readonly geometry = input<ModelGeometry | null>(null);
    readonly textures = input<ReadonlyMap<number, ModelTexture>>(new Map());
    readonly selectionKey = input("");
    readonly wireframe = signal(false);
    readonly grid = signal(false);
    readonly error = signal<string | null>(null);
    private readonly viewport = viewChild.required<ElementRef<HTMLElement>>("viewport");
    private readonly scene = signal<ModelScene | null>(null);
    private destroyed = false;
    private fittedSelection: string | null = null;

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
    }
    orbit(x: number, y: number): void { this.scene()?.orbit(x, y); }
    pan(x: number, y: number): void { this.scene()?.pan(x, y); }
    zoom(factor: number): void { this.scene()?.zoom(factor); }
    reset(): void { this.scene()?.resetCamera(); }
    key(event: KeyboardEvent): void {
        const arrows: Record<string, [number, number]> = { ArrowLeft: [-1, 0], ArrowRight: [1, 0], ArrowUp: [0, -1], ArrowDown: [0, 1] };
        const direction = arrows[event.key];
        if (direction) { event.preventDefault(); if (event.shiftKey) this.pan(direction[0], -direction[1]); else this.orbit(direction[0] * 0.2, direction[1] * 0.2); }
        else if (event.key === "+" || event.key === "=") { event.preventDefault(); this.zoom(0.8); }
        else if (event.key === "-") { event.preventDefault(); this.zoom(1.25); }
        else if (event.key === "Home") { event.preventDefault(); this.reset(); }
    }
    ngOnDestroy(): void { this.destroyed = true; this.scene()?.dispose(); }
}
