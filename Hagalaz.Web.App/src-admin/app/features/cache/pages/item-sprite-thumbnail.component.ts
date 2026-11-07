import { Component, DestroyRef, effect, inject, input, signal } from "@angular/core";
import { CacheItemsService } from "../services/cache-items.service";

/** Server-rendered item icon, requested at the displayed size so it stays sharp. */
@Component({
    selector: "cache-item-sprite-thumbnail",
    template: `@if (imageUrl(); as source) { <img [src]="source" [alt]="'Item sprite for ' + (name() || 'item #' + id())" loading="lazy" [width]="size()" [height]="size()" /> } @else if (failed()) { <span role="img" [attr.aria-label]="'Item sprite unavailable for item #' + id()">!</span> } @else { <span role="status" aria-label="Loading item sprite">…</span> }`,
    host: {"[style.width.px]": "size()", "[style.height.px]": "size()"},
    styles: `:host { display: inline-grid; place-items: center; width: 44px; height: 44px; vertical-align: middle; } img { max-width: 100%; max-height: 100%; image-rendering: pixelated; object-fit: contain; }`,
})
export class ItemSpriteThumbnailComponent {
    readonly id = input.required<number>();
    readonly name = input("");
    readonly size = input(44);
    readonly outline = input<0 | 1 | 2 | undefined>(undefined);
    /** Six-digit RGB colour or "none"; undefined keeps the inventory shadow. */
    readonly shadow = input<string | undefined>(undefined);
    readonly quantity = input<number | undefined>(undefined);
    /** Any value that changes when the item definition changes, to fetch the updated icon. */
    readonly revision = input<unknown>(undefined);
    readonly imageUrl = signal<string | null>(null);
    readonly failed = signal(false);
    private readonly items = inject(CacheItemsService);
    private objectUrl: string | null = null;
    private shownId: number | null = null;

    constructor() {
        inject(DestroyRef).onDestroy(() => this.replaceImage(null));
        effect((onCleanup) => {
            const id = this.id();
            this.revision();
            // Render at device pixels within the server's 8..512 limit.
            const pixels = Math.min(512, Math.max(8, Math.round(this.size() * (globalThis.devicePixelRatio || 1))));
            const options = { width: pixels, height: pixels, outline: this.outline(), shadow: this.shadow(), quantity: this.quantity() };
            this.failed.set(false);
            // A different item clears the icon; option and revision changes keep it visible until the replacement arrives.
            if (id !== this.shownId) this.replaceImage(null);
            this.shownId = id;
            const request = this.items.getItemSpriteImage(id, options).subscribe({
                next: (image) => this.replaceImage(URL.createObjectURL(image)),
                error: () => {
                    this.replaceImage(null);
                    this.failed.set(true);
                },
            });
            onCleanup(() => request.unsubscribe());
        });
    }

    private replaceImage(url: string | null): void {
        const previous = this.objectUrl;
        this.objectUrl = url;
        this.imageUrl.set(url);
        if (previous) URL.revokeObjectURL(previous);
    }
}
