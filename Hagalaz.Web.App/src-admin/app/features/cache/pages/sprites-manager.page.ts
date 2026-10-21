import { JsonPipe } from "@angular/common";
import { ChangeDetectionStrategy, Component, type OnDestroy, inject, signal } from "@angular/core";
import { form, FormField, max, min } from "@angular/forms/signals";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatDividerModule } from "@angular/material/divider";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { firstValueFrom } from "rxjs";
import { CacheSpritesService } from "../services/cache-sprites.service";

@Component({
    standalone: true,
    selector: "sprites-manager-page",
    imports: [FormField, JsonPipe, MatButtonModule, MatCardModule, MatDividerModule, MatFormFieldModule, MatInputModule, MatProgressBarModule],
    templateUrl: "./sprites-manager.page.html",
    styleUrl: "./sprites-manager.page.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SpritesManagerPageComponent implements OnDestroy {
    private readonly spritesService = inject(CacheSpritesService);

    readonly spriteInfo = signal<unknown>(null);
    readonly searchResult = signal<unknown>(null);
    readonly mutationResult = signal<unknown>(null);
    readonly imageUrl = signal<string | null>(null);
    readonly error = signal<string | null>(null);
    readonly loading = signal(false);

    private readonly spriteIdModel = signal({ id: 0 });
    readonly spriteIdForm = form(this.spriteIdModel, (path) => min(path.id, 0));
    private readonly spriteSearchModel = signal({ query: "", offset: 0, limit: 10 });
    readonly spriteSearchForm = form(this.spriteSearchModel, (path) => {
        min(path.offset, 0);
        min(path.limit, 1);
        max(path.limit, 200);
    });
    private readonly spriteImageModel = signal({ id: 0, frame: 0 });
    readonly spriteImageForm = form(this.spriteImageModel, (path) => {
        min(path.id, 0);
        min(path.frame, 0);
    });
    private readonly spriteUploadModel = signal({ id: 0 });
    readonly spriteUploadForm = form(this.spriteUploadModel, (path) => min(path.id, 0));

    private selectedFile: File | null = null;

    ngOnDestroy(): void {
        this.clearImageUrl();
    }
    async loadSprite(): Promise<void> {
        await this.run(async () => {
            const v = this.spriteIdModel();
            this.spriteInfo.set(await firstValueFrom(this.spritesService.getSprite(v.id)));
        });
    }
    async searchSprites(): Promise<void> {
        await this.run(async () => {
            const v = this.spriteSearchModel();
            this.searchResult.set(await firstValueFrom(this.spritesService.searchSprites(v.query, v.offset, v.limit)));
        });
    }
    async previewSpriteImage(): Promise<void> {
        await this.run(async () => {
            const v = this.spriteImageModel();
            const blob = await firstValueFrom(this.spritesService.getSpriteImage(v.id, v.frame));
            this.clearImageUrl();
            this.imageUrl.set(URL.createObjectURL(blob));
        });
    }
    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        this.selectedFile = input.files?.[0] ?? null;
    }

    async uploadSpriteImage(): Promise<void> {
        const selectedFile = this.selectedFile;
        if (!selectedFile) {
            this.error.set("Select a PNG file before uploading.");
            return;
        }

        await this.run(async () => {
            const v = this.spriteUploadModel();
            this.mutationResult.set(await firstValueFrom(this.spritesService.replaceSpriteImage(v.id, selectedFile)));
        });
    }

    private clearImageUrl(): void {
        const current = this.imageUrl();
        if (current) {
            URL.revokeObjectURL(current);
            this.imageUrl.set(null);
        }
    }

    private async run(action: () => Promise<void>): Promise<void> {
        this.error.set(null);
        this.loading.set(true);
        try {
            await action();
        } catch (error: unknown) {
            const response = error as { error?: { detail?: string; title?: string }; message?: string };
            this.error.set(response.error?.detail ?? response.error?.title ?? response.message ?? "Request failed.");
        } finally {
            this.loading.set(false);
        }
    }
}
