import { JsonPipe } from "@angular/common";
import { ChangeDetectionStrategy, Component, effect, inject, signal } from "@angular/core";
import { FormField, form, max, min, required } from "@angular/forms/signals";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatExpansionModule } from "@angular/material/expansion";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatTabsModule } from "@angular/material/tabs";
import { CacheSpritesStore } from "../stores/cache-sprites.store";

@Component({
    standalone: true,
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
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SpritesManagerPageComponent {
    readonly store = inject(CacheSpritesStore);

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
        required(path.id);
        min(path.id, 0);
        required(path.frame);
        min(path.frame, 0);
    });
    private readonly spriteUploadModel = signal({ id: 0 });
    readonly spriteUploadForm = form(this.spriteUploadModel, (path) => {
        required(path.id);
        min(path.id, 0);
    });

    readonly selectedFile = signal<File | null>(null);
    readonly imageUrl = signal<string | null>(null);

    constructor() {
        effect((onCleanup) => {
            const blob = this.store.imageBlob();
            if (!blob) return;
            const url = URL.createObjectURL(blob);
            this.imageUrl.set(url);
            onCleanup(() => URL.revokeObjectURL(url));
        });
    }

    async loadSprite(): Promise<void> {
        if (this.spriteIdForm().invalid()) return;
        const v = this.spriteIdModel();
        await this.store.loadSprite(v.id);
    }

    async searchSprites(): Promise<void> {
        if (this.spriteSearchForm().invalid()) return;
        const v = this.spriteSearchModel();
        await this.store.searchSprites(v.query, v.offset, v.limit);
    }

    async previewSpriteImage(): Promise<void> {
        if (this.spriteImageForm().invalid()) return;
        const v = this.spriteImageModel();
        await this.store.previewSpriteImage(v.id, v.frame);
    }

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        this.selectedFile.set(input.files?.[0] ?? null);
        this.store.clearError();
    }

    async uploadSpriteImage(): Promise<void> {
        if (this.spriteUploadForm().invalid() || this.store.loading()) return;
        await this.store.uploadSpriteImage(this.spriteUploadModel().id, this.selectedFile());
    }
}
