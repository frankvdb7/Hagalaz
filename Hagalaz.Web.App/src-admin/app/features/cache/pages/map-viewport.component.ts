import { afterNextRender, Component, effect, type ElementRef, input, type OnDestroy, signal, viewChild } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import type { ModelGeometry, ModelScene, ModelTexture } from "../../../../../shared/model-viewer/model-scene";

@Component({
    selector: "cache-map-viewport",
    imports: [MatButtonModule],
    template: `
        <div #viewport class="h-112 w-full outline-offset-2 sm:h-144" tabindex="0" role="img" aria-label="3D map viewport. Arrow keys orbit, Shift and arrows pan, plus and minus zoom, Home resets." (keydown)="key($event)"></div>
        @if (error()) { <p role="alert">{{ error() }}</p> }
        <div class="flex flex-wrap gap-2 p-3" aria-label="Map camera controls">
            <button mat-button type="button" (click)="orbit(-0.2, 0)">Orbit left</button><button mat-button type="button" (click)="orbit(0.2, 0)">Orbit right</button>
            <button mat-button type="button" (click)="orbit(0, -0.2)">Orbit up</button><button mat-button type="button" (click)="orbit(0, 0.2)">Orbit down</button>
            <button mat-button type="button" (click)="pan(-1, 0)">Pan left</button><button mat-button type="button" (click)="pan(1, 0)">Pan right</button>
            <button mat-button type="button" (click)="pan(0, 1)">Pan up</button><button mat-button type="button" (click)="pan(0, -1)">Pan down</button>
            <button mat-button type="button" (click)="zoom(0.8)">Zoom in</button><button mat-button type="button" (click)="zoom(1.25)">Zoom out</button>
            <button mat-stroked-button type="button" (click)="reset()">Reset camera</button>
        </div>`,
    host: { class: "block min-w-0" },
})
export class MapViewportComponent implements OnDestroy {
    readonly geometry = input<ModelGeometry | null>(null);
    readonly textures = input<ReadonlyMap<number, ModelTexture>>(new Map());
    readonly selectionKey = input("");
    readonly error = signal<string | null>(null);
    private readonly viewport = viewChild.required<ElementRef<HTMLElement>>("viewport");
    private readonly scene = signal<ModelScene | null>(null);
    private destroyed = false;
    private fittedSelection: string | null = null;

    constructor() {
        afterNextRender(async () => {
            try {
                const { ModelScene } = await import("../../../../../shared/model-viewer/model-scene");
                if (!this.destroyed) this.scene.set(new ModelScene(this.viewport().nativeElement, "Interactive 3D map regions"));
            } catch { this.error.set("3D rendering is unavailable. Enable WebGL in a supported browser."); }
        });
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
