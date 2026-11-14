import { HttpClient } from "@angular/common/http";
import { afterRenderEffect, Component, computed, effect, type ElementRef, inject, Injectable, input, signal, viewChild } from "@angular/core";
import { rxResource } from "@angular/core/rxjs-interop";
import { type Observable, shareReplay, tap } from "rxjs";
import { environment } from "../../../../../environments/environment";
import type { WidgetLayer } from "./widget-layout";
import { WidgetLiveModelsService } from "./widget-live-models.service";

@Injectable()
export class WidgetModelImages {
    private readonly http = inject(HttpClient);
    private readonly requests = new Map<string, Observable<Blob>>();

    get(path: string) {
        let request = this.requests.get(path);
        if (!request) {
            request = this.http
                .get(`${environment.cacheApiUrl}${path}`, { responseType: "blob" })
                .pipe(tap({ error: () => this.requests.delete(path) }), shareReplay({ bufferSize: 1, refCount: true }));
            this.requests.set(path, request);
        }
        return request;
    }
}

function imageSize(value: number) {
    return Math.max(8, Math.min(value, 512));
}

/** Server image path for a widget model layer, using its authored camera when one is set. */
function widgetModelImagePath({ component, width, height }: WidgetLayer): string | null {
    const modelId = component.modelId ?? -1;
    if (modelId < 0 || width < 1 || height < 1) return null;
    const dimensions = { width: imageSize(width), height: imageSize(height) };
    const camera = component.modelCamera;
    const authored = camera !== undefined && camera.zoom > 0;
    const query = new URLSearchParams();
    for (const [key, value] of Object.entries(authored ? { ...dimensions, ...camera } : dimensions)) query.set(key, String(value));
    return `models/${modelId}/${authored ? "widget-image" : "image"}?${query}`;
}

@Component({
    selector: "cache-widget-model",
    template: `
        @if (liveAsset()) {
            <canvas #target [width]="width()" [height]="height()" aria-hidden="true"></canvas>
        } @else if (url(); as source) { <img [src]="source" alt="" /> }
    `,
    styles: `:host, img, canvas { display: block; width: 100%; height: 100%; } :host(.unavailable) { border: 1px dashed var(--workspace-muted); }`,
    host: { "[class.unavailable]": "!liveAsset() && !url()" },
})
export class WidgetModelComponent {
    private readonly images = inject(WidgetModelImages);
    private readonly live = inject(WidgetLiveModelsService);
    readonly layer = input.required<WidgetLayer>();
    readonly url = signal<string | null>(null);
    private readonly target = viewChild<ElementRef<HTMLCanvasElement>>("target");
    private readonly id = computed(() => this.layer().component.modelId ?? -1);
    private readonly animationId = computed(() => this.layer().component.modelAnimationId ?? -1);
    private readonly camera = computed(() => this.layer().component.modelCamera);
    readonly width = computed(() => Math.min(this.layer().width, 1024));
    readonly height = computed(() => Math.min(this.layer().height, 1024));
    private readonly model = rxResource({
        params: () => (this.live.available() && this.id() >= 0 ? this.id() : undefined),
        stream: ({ params }) => this.live.model(params),
    });
    readonly liveAsset = computed(() => (this.live.available() && this.width() > 0 && this.height() > 0 && this.model.hasValue() ? this.model.value() : null));
    private readonly animation = rxResource({
        params: () => (this.liveAsset() && this.animationId() >= 0 ? this.animationId() : undefined),
        stream: ({ params }) => this.live.animation(params),
    });
    /** Falls back to a server-rendered image when live rendering is unavailable or the model cannot be drawn live. */
    private readonly usesServerImage = computed(() => this.live.available() === false || (this.model.hasValue() && this.model.value() === null));
    private readonly path = computed(() => (this.usesServerImage() ? widgetModelImagePath(this.layer()) : null));

    constructor() {
        afterRenderEffect(() => {
            if (this.id() >= 0 && this.width() > 0 && this.height() > 0) void this.live.start();
        });
        afterRenderEffect((cleanup) => {
            const canvas = this.target()?.nativeElement;
            const asset = this.liveAsset();
            if (!canvas || !asset) return;
            const pose = this.animation.hasValue() ? this.animation.value() : undefined;
            this.live.enqueue(canvas, { asset, camera: this.camera(), width: this.width(), height: this.height(), pose });
            cleanup(() => this.live.remove(canvas));
        });
        effect((cleanup) => {
            const path = this.path();
            this.url.set(null);
            if (!path) return;
            let source: string | undefined;
            const request = this.images.get(path).subscribe({
                next: (image) => {
                    source = URL.createObjectURL(image);
                    this.url.set(source);
                },
                error: () => this.url.set(null),
            });
            cleanup(() => {
                request.unsubscribe();
                if (source) URL.revokeObjectURL(source);
            });
        });
    }
}
