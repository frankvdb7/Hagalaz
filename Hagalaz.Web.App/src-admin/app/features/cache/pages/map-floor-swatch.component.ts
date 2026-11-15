import { afterNextRender, Component, computed, DestroyRef, ElementRef, effect, inject, input, signal } from "@angular/core";
import { CacheMapsService } from "../services/cache-maps.service";
import type { MapFloor } from "../services/map-render.models";

/** A floor's texture, requested once its row nears the viewport, over the floor's colour. */
@Component({
    selector: "cache-map-floor-swatch",
    template: `@if (imageUrl(); as source) { <img [src]="source" alt="" width="48" height="48" /> }`,
    styles: `
        :host { display: block; flex: none; overflow: hidden; width: 48px; height: 48px; border: 1px solid var(--mat-sys-outline-variant); border-radius: 4px; }
        img { display: block; width: 100%; height: 100%; object-fit: cover; }
    `,
    host: { "[style.background-color]": "color()" },
})
export class MapFloorSwatchComponent {
    readonly floor = input.required<MapFloor>();
    /** A texture-only floor has no colour of its own to show while its image loads. */
    readonly color = computed(() => {
        const { rgb, textureId } = this.floor();
        return !rgb && textureId >= 0 ? "transparent" : `#${(rgb ?? 0).toString(16).padStart(6, "0")}`;
    });
    readonly imageUrl = signal<string | null>(null);
    private readonly maps = inject(CacheMapsService);
    private readonly visible = signal(false);
    private objectUrl: string | null = null;

    constructor() {
        const destroy = inject(DestroyRef);
        const host = inject<ElementRef<HTMLElement>>(ElementRef);
        destroy.onDestroy(() => this.replaceImage(null));
        afterNextRender(() => {
            const observer = new IntersectionObserver((entries) => {
                if (!entries.some((entry) => entry.isIntersecting)) return;
                this.visible.set(true);
                observer.disconnect();
            }, { rootMargin: "200px" });
            observer.observe(host.nativeElement);
            destroy.onDestroy(() => observer.disconnect());
        });
        effect((onCleanup) => {
            const texture = this.floor().textureId;
            this.replaceImage(null);
            if (texture < 0 || !this.visible()) return;
            const request = this.maps.texture(texture).subscribe({ next: ({ blob }) => this.replaceImage(URL.createObjectURL(blob)), error: () => this.replaceImage(null) });
            onCleanup(() => request.unsubscribe());
        });
    }

    private replaceImage(url: string | null): void {
        if (this.objectUrl) URL.revokeObjectURL(this.objectUrl);
        this.objectUrl = url;
        this.imageUrl.set(url);
    }
}
