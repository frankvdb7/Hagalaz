import { Component, signal, viewChild } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { CacheModelPreviewComponent } from "./cache-model-preview.component";

@Component({
    selector: "cache-models-manager-page",
    imports: [FormsModule, CacheModelPreviewComponent],
    template: `<h2>Model viewer</h2><form (ngSubmit)="load()"><label>Model ID <input name="modelId" type="number" min="0" [(ngModel)]="draftId" required></label><button type="submit">Load model</button></form>
        @if (selectedId() !== null) { <h3>Model #{{selectedId()}}</h3><cache-model-preview [modelId]="selectedId()" /> }
        @if (selectedId() === null) { <p>Enter a cache model ID to inspect its geometry. Drag to orbit and scroll to zoom.</p> }`,
    styles: `form { display: flex; flex-wrap: wrap; gap: 12px; margin: 16px 0; } input { color: inherit; background: transparent; border: 1px solid currentColor; padding: 8px; width: 120px; } button { padding: 8px 16px; }`,
})
export class ModelsManagerPageComponent {
    draftId = 0;
    readonly selectedId = signal<number | null>(null);
    private readonly preview = viewChild(CacheModelPreviewComponent);
    load(): void {
        if (!Number.isSafeInteger(this.draftId) || this.draftId < 0) return;
        if (this.selectedId() === this.draftId) this.preview()?.model.reload();
        else this.selectedId.set(this.draftId);
    }
}
