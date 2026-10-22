import { JsonPipe } from "@angular/common";
import { Component, effect, inject, input, output, signal, untracked } from "@angular/core";
import { FormField, form, max, min, readonly as readOnly, required } from "@angular/forms/signals";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDialog } from "@angular/material/dialog";
import { MatExpansionModule } from "@angular/material/expansion";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatTabsModule } from "@angular/material/tabs";
import { firstValueFrom } from "rxjs";
import type { SpriteMutationResultDto } from "../services/cache.models";
import { CacheSpritesStore } from "../stores/cache-sprites.store";
import { CacheWriteDialogComponent } from "./cache-write-dialog";

@Component({
    selector: "sprites-manager-page",
    imports: [
        FormField,
        JsonPipe,
        MatButtonModule,
        MatCardModule,
        MatExpansionModule,
        MatIconModule,
        MatTabsModule,
        MatFormFieldModule,
        MatInputModule,
        MatProgressBarModule,
    ],
    providers: [CacheSpritesStore],
    templateUrl: "./sprites-manager.page.html",
    styleUrl: "./sprites-manager.page.scss",
})
export class SpritesManagerPageComponent {
    readonly store = inject(CacheSpritesStore);
    readonly spriteId = input<number | null>(null);
    readonly imageReplaced = output<SpriteMutationResultDto>();
    private readonly dialog = inject(MatDialog);

    private readonly spriteIdModel = signal({ id: 0 });
    readonly spriteIdForm = form(this.spriteIdModel, (path) => {
        required(path.id);
        min(path.id, 0);
    });
    private readonly spriteSearchModel = signal({ query: "", offset: 0, limit: 10 });
    readonly spriteSearchForm = form(this.spriteSearchModel, (path) => {
        required(path.offset);
        min(path.offset, 0);
        required(path.limit);
        min(path.limit, 1);
        max(path.limit, 200);
    });
    private readonly spriteImageModel = signal({ id: 0, frame: 0 });
    readonly spriteImageForm = form(this.spriteImageModel, (path) => {
        readOnly(path.id, () => this.spriteId() !== null);
        required(path.id);
        min(path.id, 0);
        required(path.frame);
        min(path.frame, 0);
    });
    private readonly spriteUploadModel = signal({ id: 0 });
    readonly spriteUploadForm = form(this.spriteUploadModel, (path) => {
        readOnly(path.id, () => this.spriteId() !== null);
        required(path.id);
        min(path.id, 0);
    });

    readonly selectedFile = signal<File | null>(null);
    readonly imageUrl = signal<string | null>(null);

    constructor() {
        effect(() => {
            const id = this.spriteId();
            if (id === null) return;
            untracked(() => {
                this.spriteImageModel.set({ id, frame: 0 });
                this.spriteUploadModel.set({ id });
                this.previewSpriteImage();
            });
        });
        effect(() => {
            const result = this.store.mutationResult();
            if (!result || this.spriteId() === null) return;
            untracked(() => {
                this.spriteImageModel.set({ id: result.id, frame: 0 });
                this.previewSpriteImage();
                this.imageReplaced.emit(result);
            });
        });
        effect((onCleanup) => {
            const blob = this.store.imageBlob();
            if (!blob) return;
            const url = URL.createObjectURL(blob);
            this.imageUrl.set(url);
            onCleanup(() => URL.revokeObjectURL(url));
        });
    }

    loadSprite(): void {
        if (this.spriteIdForm().invalid()) return;
        const v = this.spriteIdModel();
        this.store.loadSprite(v.id);
    }

    searchSprites(): void {
        if (this.spriteSearchForm().invalid()) return;
        const v = this.spriteSearchModel();
        this.store.searchSprites(v);
    }

    previewSpriteImage(): void {
        if (this.spriteImageForm().invalid()) return;
        const v = this.spriteImageModel();
        this.store.previewSpriteImage(v);
    }

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        this.selectedFile.set(input.files?.[0] ?? null);
        this.store.clearError();
    }

    async uploadSpriteImage(): Promise<void> {
        if (this.spriteUploadForm().invalid() || this.store.loading()) return;
        const file = this.selectedFile();
        if (!file) {
            this.store.uploadSpriteImage({ id: this.spriteUploadModel().id, file: null });
            return;
        }
        const id = this.spriteUploadModel().id;
        const confirmed = await firstValueFrom(
            this.dialog.open(CacheWriteDialogComponent, { data: `Replace sprite #${id} with ${file.name}?`, maxWidth: "32rem" }).afterClosed()
        );
        if (confirmed === true) this.store.uploadSpriteImage({ id, file });
    }
}
