import { Component, computed, effect, inject, input, signal } from "@angular/core";
import { CacheSpritesStore } from "../stores/cache-sprites.store";

@Component({
    selector: "cache-sprite-frame-thumbnail",
    providers: [CacheSpritesStore],
    template: `@if (currentUrl(); as source) { <img [src]="source" alt="" /> } @else { <span [attr.aria-hidden]="frame() !== null">{{ placeholder() }}</span> }`,
    styles: `:host { display: grid; place-items: center; width: 100%; height: 48px; } img { max-width: 100%; max-height: 48px; image-rendering: pixelated; object-fit: contain; }`,
})
export class SpriteFrameThumbnailComponent {
    readonly id = input.required<number>();
    readonly frame = input.required<number | null>();
    readonly revision = input<unknown>();
    readonly store = inject(CacheSpritesStore);
    readonly url = signal<string | null>(null);
    readonly currentUrl = computed(() => (this.store.imageId() === this.id() && this.store.imageFrame() === this.frame() ? this.url() : null));
    readonly placeholder = computed(() => (this.frame() === null ? "No frames" : this.store.previewError() ? "!" : "…"));

    constructor() {
        effect(() => {
            this.revision();
            const frame = this.frame();
            this.store.previewSpriteImage(frame === null ? null : { id: this.id(), frame });
        });
        effect((onCleanup) => {
            const blob = this.store.imageBlob();
            this.url.set(null);
            if (!blob) return;
            const url = URL.createObjectURL(blob);
            this.url.set(url);
            onCleanup(() => URL.revokeObjectURL(url));
        });
    }
}
