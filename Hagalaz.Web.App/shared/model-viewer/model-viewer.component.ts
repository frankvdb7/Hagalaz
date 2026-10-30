import { afterNextRender, Component, effect, ElementRef, input, OnDestroy, signal, viewChild } from "@angular/core";
import type { ModelGeometry, ModelTexture } from "./model-scene";

@Component({
    selector: "hagalaz-model-viewer",
    template: `<div #viewport class="viewport"></div>@if (error()) { <p role="alert">{{ error() }}</p> }<button type="button" (click)="reset()">Reset camera</button>`,
    styles: `:host { display: block; } .viewport { width: 100%; height: 360px; min-width: 0; } button { display: block; margin: 8px 0 12px; padding: 8px 12px; border: 1px solid currentColor; border-radius: 4px; }`,
})
export class ModelViewerComponent implements OnDestroy {
    readonly geometry = input<ModelGeometry | null>(null);
    readonly textures = input<ReadonlyMap<number, ModelTexture>>(new Map());
    readonly error = signal<string | null>(null);
    private readonly viewport = viewChild.required<ElementRef<HTMLElement>>("viewport");
    private readonly scene = signal<import("./model-scene").ModelScene | null>(null);
    private destroyed = false;

    constructor() {
        afterNextRender(async () => {
            try {
                const { ModelScene } = await import("./model-scene");
                if (!this.destroyed) this.scene.set(new ModelScene(this.viewport().nativeElement));
            } catch {
                this.error.set("3D rendering is unavailable. Check that your browser supports WebGL.");
            }
        });
        effect(() => {
            const scene = this.scene();
            const geometry = this.geometry();
            if (scene && geometry) { scene.setTextures(this.textures()); scene.setGeometry(geometry); }
        });
    }

    reset(): void { this.scene()?.resetCamera(); }
    ngOnDestroy(): void { this.destroyed = true; this.scene()?.dispose(); }
}
