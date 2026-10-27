import { Component, effect, inject, input, signal } from "@angular/core";
import { CacheItemsService } from "../services/cache-items.service";

@Component({
    selector: "cache-item-sprite-thumbnail",
    template: `@if (imageUrl(); as source) { <img [src]="source" [alt]="'Item sprite for ' + (name() || 'item #' + id())" loading="lazy" width="48" height="48" /> } @else if (failed()) { <span role="img" [attr.aria-label]="'Item sprite unavailable for item #' + id()">!</span> } @else { <span role="status" aria-label="Loading item sprite">…</span> }`,
    styles: `:host { display: inline-grid; place-items: center; width: 48px; height: 48px; } img { max-width: 100%; max-height: 100%; image-rendering: pixelated; object-fit: contain; }`,
})
export class ItemSpriteThumbnailComponent {
    readonly id = input.required<number>();
    readonly name = input("");
    readonly imageUrl = signal<string | null>(null);
    readonly failed = signal(false);
    private readonly items = inject(CacheItemsService);

    constructor() {
        effect((onCleanup) => {
            const id = this.id();
            this.imageUrl.set(null);
            this.failed.set(false);
            let objectUrl: string | null = null;
            const request = this.items.getItemSpriteImage(id).subscribe({
                next: (image) => {
                    objectUrl = URL.createObjectURL(image);
                    this.imageUrl.set(objectUrl);
                },
                error: () => this.failed.set(true),
            });
            onCleanup(() => {
                request.unsubscribe();
                if (objectUrl) URL.revokeObjectURL(objectUrl);
            });
        });
    }
}
