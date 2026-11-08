import { afterNextRender, Component, DestroyRef, ElementRef, effect, inject, input, signal } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import { CacheObjectsService } from "../services/cache-objects.service";

const displayWidth = 96;
const displayHeight = 64;

/** Server-rendered object preview, requested once the tile nears the viewport. */
@Component({
    selector: "cache-object-model-thumbnail",
    imports: [MatIconModule],
    template: `@if (imageUrl(); as source) {
        <img [src]="source" [alt]="name()" [width]="width" [height]="height" />
    } @else {
        <mat-icon aria-hidden="true">view_in_ar</mat-icon>
    }`,
    styles: `
        :host { display: grid; place-items: center; width: 96px; height: 64px; color: var(--mat-sys-on-surface-variant); }
        img { display: block; object-fit: contain; }
    `,
})
export class ObjectModelThumbnailComponent {
    readonly id = input.required<number>();
    /** The object's placeable shape; objects without one have no model to draw. */
    readonly shape = input.required<number | null>();
    readonly name = input("");
    readonly width = displayWidth;
    readonly height = displayHeight;
    readonly imageUrl = signal<string | null>(null);
    private readonly objects = inject(CacheObjectsService);
    private readonly visible = signal(false);
    private objectUrl: string | null = null;

    constructor() {
        const destroy = inject(DestroyRef);
        const host = inject<ElementRef<HTMLElement>>(ElementRef);
        destroy.onDestroy(() => this.replaceImage(null));
        afterNextRender(() => {
            const observer = new IntersectionObserver(
                (entries) => {
                    if (!entries.some((entry) => entry.isIntersecting)) return;
                    this.visible.set(true);
                    observer.disconnect();
                },
                { rootMargin: "200px" }
            );
            observer.observe(host.nativeElement);
            destroy.onDestroy(() => observer.disconnect());
        });
        effect((onCleanup) => {
            const id = this.id();
            const shape = this.shape();
            this.replaceImage(null);
            if (shape === null || !this.visible()) return;
            const scale = globalThis.devicePixelRatio || 1;
            const request = this.objects
                .getObjectImage(id, { width: Math.min(512, Math.round(displayWidth * scale)), height: Math.min(512, Math.round(displayHeight * scale)), shape })
                .subscribe({ next: (image) => this.replaceImage(URL.createObjectURL(image)), error: () => this.replaceImage(null) });
            onCleanup(() => request.unsubscribe());
        });
    }

    private replaceImage(url: string | null): void {
        if (this.objectUrl) URL.revokeObjectURL(this.objectUrl);
        this.objectUrl = url;
        this.imageUrl.set(url);
    }
}
