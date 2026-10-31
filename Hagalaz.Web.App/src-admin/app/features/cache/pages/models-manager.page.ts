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
    imports: [FormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, CacheModelPreviewComponent],
    template: `<section aria-label="Model workspace">
        <mat-card appearance="outlined" class="mb-5"><mat-card-content>
            <form class="pt-1" (ngSubmit)="load()">
                <div class="flex flex-col gap-3 sm:flex-row">
                    <mat-form-field appearance="outline" subscriptSizing="dynamic" class="grow"><mat-label>Model ID</mat-label><mat-icon matPrefix>search</mat-icon><input matInput name="modelId" type="number" min="0" step="1" [(ngModel)]="draftId" required #modelId="ngModel" /><mat-error>Enter a model ID of zero or greater.</mat-error></mat-form-field>
                    <button mat-flat-button type="submit" class="shrink-0 self-center whitespace-nowrap" [disabled]="modelId.invalid"><mat-icon>search</mat-icon>Load model</button>
                </div>
                <p class="supporting-text mt-2 text-xs">Enter a cache model ID to inspect its geometry. Drag to orbit and scroll to zoom.</p>
            </form>
        </mat-card-content></mat-card>
        @if (selectedId() !== null) { <mat-card appearance="outlined"><mat-card-header><mat-card-title><h2>Model #{{selectedId()}}</h2></mat-card-title></mat-card-header><mat-card-content><cache-model-preview [modelId]="selectedId()" /></mat-card-content></mat-card> }
    </section>`,
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
