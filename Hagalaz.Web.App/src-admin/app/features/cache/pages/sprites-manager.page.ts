import { ChangeDetectionStrategy, Component, OnDestroy, inject, signal } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDividerModule } from "@angular/material/divider";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltipModule } from "@angular/material/tooltip";
import { CacheSpritesStore } from "../services/cache-sprites.store";

import { RunicCardComponent } from "../../../core/components/runic-card/runic-card.component";
import { JsonPropertyViewerComponent } from "../../../core/components/json-viewer/json-viewer.component";

@Component({
    standalone: true,
    selector: "sprites-manager-page",
    imports: [
        ReactiveFormsModule,
        MatButtonModule,
        MatCardModule,
        MatDividerModule,
        MatFormFieldModule,
        MatInputModule,
        MatProgressBarModule,
        MatProgressSpinnerModule,
        MatIconModule,
        MatTooltipModule,
        RunicCardComponent,
        JsonPropertyViewerComponent,
    ],
    templateUrl: "./sprites-manager.page.html",
    styleUrl: "./sprites-manager.page.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SpritesManagerPageComponent implements OnDestroy {
    private readonly fb = inject(FormBuilder);
    readonly store = inject(CacheSpritesStore);

    readonly spriteIdForm = this.fb.nonNullable.group({ id: [0, Validators.min(0)] });
    readonly spriteSearchForm = this.fb.nonNullable.group({ query: [""], offset: [0, Validators.min(0)], limit: [20, [Validators.min(1), Validators.max(200)]] });
    readonly spriteImageForm = this.fb.nonNullable.group({ id: [0, Validators.min(0)], frame: [0, Validators.min(0)] });
    readonly spriteUploadForm = this.fb.nonNullable.group({ id: [0, Validators.min(0)] });

    private selectedFile: File | null = null;
    readonly selectedFileName = signal<string | null>(null);

    ngOnDestroy(): void {
        this.store.clearImageUrl();
    }

    async loadSprite(): Promise<void> {
        const { id } = this.spriteIdForm.getRawValue();
        if (id === 0) return;
        this.store.loadSprite(id);
        this.store.previewSpriteImage({ id, frame: 0 }); // Default frame
    }

    async searchSprites(): Promise<void> {
        const { query, offset, limit } = this.spriteSearchForm.getRawValue();
        this.store.searchSprites({ query, offset, limit });
    }

    async previewSpriteImage(): Promise<void> {
        const { id, frame } = this.spriteImageForm.getRawValue();
        // Use ID from either form
        const targetId = id || this.spriteIdForm.getRawValue().id;
        this.store.previewSpriteImage({ id: targetId, frame });
    }

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        this.selectedFile = input.files?.[0] ?? null;
        this.selectedFileName.set(this.selectedFile?.name ?? null);
    }

    async uploadSpriteImage(): Promise<void> {
        if (!this.selectedFile) {
            this.store.setError("Select a PNG file before uploading.");
            return;
        }

        const { id } = this.spriteUploadForm.getRawValue();
        this.store.uploadSpriteImage({ id, file: this.selectedFile });
    }
}
