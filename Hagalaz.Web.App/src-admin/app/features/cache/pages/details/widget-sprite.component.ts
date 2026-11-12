import { Component, effect, inject, input, signal } from "@angular/core";
import { CacheSpritesService } from "../../services/cache-sprites.service";

@Component({
    selector: "cache-widget-sprite",
    template: `
        @if (url(); as source) {
            @if (tiled()) {
                <div class="tiled" [style.background-image]="'url(' + source + ')'" aria-hidden="true"></div>
            } @else {
                <img [src]="source" alt="" />
            }
        }
    `,
    styles: `:host, img, .tiled { display: block; width: 100%; height: 100%; image-rendering: pixelated; }`,
})
export class WidgetSpriteComponent {
    private readonly sprites = inject(CacheSpritesService);
    readonly id = input.required<number>();
    readonly tiled = input(false);
    readonly url = signal<string | null>(null);

    constructor() {
        effect((cleanup) => {
            const id = this.id();
            let source: string | undefined;
            this.url.set(null);
            const request = this.sprites.getSpriteImage(id, 0).subscribe({
                next: (blob) => {
                    source = URL.createObjectURL(blob);
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
