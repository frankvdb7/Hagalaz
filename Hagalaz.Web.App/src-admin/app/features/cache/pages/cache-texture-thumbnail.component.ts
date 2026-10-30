import { HttpClient } from "@angular/common/http";
import { Component, effect, inject, input, signal } from "@angular/core";
import { rxResource } from "@angular/core/rxjs-interop";
import { environment } from "../../../../environments/environment";

@Component({
    selector: "cache-texture-thumbnail",
    template: `<figure>@if (url(); as source) { <img [src]="source" [alt]="'Texture ' + id()" /> } @else if (image.error()) { <span>Image unavailable</span> } @else { <span>Loading texture…</span> }<figcaption>Texture #{{id()}}</figcaption></figure>`,
    styles: `:host { display: inline-block; margin: 8px; } figure { margin: 0; } img { width: 96px; height: 96px; object-fit: contain; }`,
})
export class CacheTextureThumbnailComponent {
    readonly id = input.required<number>();
    private readonly http = inject(HttpClient);
    readonly image = rxResource({ params: () => this.id(), stream: ({ params }) => this.http.get(`${environment.cacheApiUrl}textures/${params}/image`, { responseType: "blob" }) });
    readonly url = signal<string | null>(null);
    constructor() {
        effect(cleanup => {
            const blob = this.image.value();
            this.url.set(null);
            if (!blob) return;
            const url = URL.createObjectURL(blob);
            this.url.set(url);
            cleanup(() => URL.revokeObjectURL(url));
        });
    }
}
