import { NgTemplateOutlet } from "@angular/common";
import { Component, signal, viewChild } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { CacheModelPreviewComponent } from "./cache-model-preview.component";

@Component({
    selector: "cache-models-manager-page",
    imports: [NgTemplateOutlet, FormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, CacheModelPreviewComponent],
    template: `<section aria-label="Model workspace">
        <ng-template #lookup>
            <form (ngSubmit)="load()" class="flex flex-wrap items-center gap-2">
                <mat-form-field appearance="outline" subscriptSizing="dynamic" class="model-lookup-field"><mat-label>Model ID</mat-label><mat-icon matPrefix>search</mat-icon><input matInput name="modelId" type="number" min="0" step="1" [(ngModel)]="draftId" required #modelId="ngModel" /><mat-error>Enter a model ID of zero or greater.</mat-error></mat-form-field>
                <button mat-flat-button type="submit" [disabled]="modelId.invalid"><mat-icon>search</mat-icon>Load model</button>
            </form>
        </ng-template>
        @if (selectedId() !== null) {
            <cache-model-preview [modelId]="selectedId()" [workspace]="true"><div modelLookup><ng-container [ngTemplateOutlet]="lookup" /></div></cache-model-preview>
        } @else {
            <mat-card appearance="outlined"><mat-card-content><ng-container [ngTemplateOutlet]="lookup" /><p class="supporting-text mt-3 text-xs">Enter a cache model ID to inspect its geometry. Drag to orbit and scroll to zoom.</p></mat-card-content></mat-card>
        }
    </section>`,
    styles: `.model-lookup-field { width: 180px; }`,
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
