import { Component, computed, input, model } from "@angular/core";
import { CacheTextureThumbnailComponent } from "./cache-texture-thumbnail.component";

@Component({
    selector: "section[cache-model-textures]",
    imports: [CacheTextureThumbnailComponent],
    template: `<div class="flex flex-wrap items-center justify-between gap-2 mb-3"><h2 class="studio-section-title">Texture images ({{ ids().length }})</h2><span class="supporting-text text-xs">Referenced by this model</span></div>
        <div class="flex flex-wrap gap-2">
            @for (id of ids(); track id) { <button type="button" class="texture-choice studio-control-surface studio-choice p-0" [class.selected]="selected() === id" [attr.aria-label]="'Inspect texture ' + id" [attr.aria-pressed]="selected() === id" (click)="selected.set(id)"><cache-texture-thumbnail [id]="id" /></button> }
            @empty { <p class="supporting-text text-sm py-3">This model does not reference any texture images.</p> }
        </div>
        @if (selected() >= 0) { <p class="supporting-text text-sm mt-3">Texture #{{ selected() }} · {{ alphaLabel() }}</p> }
        <ng-content />`,
})
export class ModelTexturesComponent {
    readonly ids = input.required<readonly number[]>();
    readonly materials = input.required<ReadonlyMap<number, { alphaMode: 0 | 1 | 2 }>>();
    readonly selected = model(-1);
    readonly alphaLabel = computed(() => {
        const material = this.materials().get(this.selected());
        if (!material) return "Material unavailable";
        return ["Opaque", "Cutout", "Blended"][material.alphaMode];
    });
}
