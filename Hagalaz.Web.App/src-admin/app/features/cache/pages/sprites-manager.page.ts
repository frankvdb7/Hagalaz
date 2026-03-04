import { JsonPipe } from "@angular/common";
import { ChangeDetectionStrategy, Component, OnDestroy, inject, signal } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
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
    imports: [ReactiveFormsModule, JsonPipe, MatButtonModule, MatCardModule, MatDividerModule, MatFormFieldModule, MatInputModule, MatProgressBarModule],
    templateUrl: "./sprites-manager.page.html",
    styleUrl: "./sprites-manager.page.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SpritesManagerPageComponent implements OnDestroy {
    private readonly fb = inject(FormBuilder);
    private readonly spritesService = inject(CacheSpritesService);

    readonly spriteInfo = signal<unknown>(null);
    readonly searchResult = signal<unknown>(null);
    readonly mutationResult = signal<unknown>(null);
    readonly imageUrl = signal<string | null>(null);
    readonly error = signal<string | null>(null);
    readonly loading = signal(false);

    readonly spriteIdForm = this.fb.nonNullable.group({ id: [0, Validators.min(0)] });
    readonly spriteSearchForm = this.fb.nonNullable.group({ query: [""], offset: [0, Validators.min(0)], limit: [10, [Validators.min(1), Validators.max(200)]] });
    readonly spriteImageForm = this.fb.nonNullable.group({ id: [0, Validators.min(0)], frame: [0, Validators.min(0)] });
    readonly spriteUploadForm = this.fb.nonNullable.group({ id: [0, Validators.min(0)] });

    private selectedFile: File | null = null;

    ngOnDestroy(): void { this.clearImageUrl(); }
    async loadSprite(): Promise<void> { await this.run(async () => { const v = this.spriteIdForm.getRawValue(); this.spriteInfo.set(await firstValueFrom(this.spritesService.getSprite(v.id))); }); }
    async searchSprites(): Promise<void> { await this.run(async () => { const v = this.spriteSearchForm.getRawValue(); this.searchResult.set(await firstValueFrom(this.spritesService.searchSprites(v.query, v.offset, v.limit))); }); }
    async previewSpriteImage(): Promise<void> { await this.run(async () => { const v = this.spriteImageForm.getRawValue(); const blob = await firstValueFrom(this.spritesService.getSpriteImage(v.id, v.frame)); this.clearImageUrl(); this.imageUrl.set(URL.createObjectURL(blob)); }); }
    onFileSelected(event: Event): void { const input = event.target as HTMLInputElement; this.selectedFile = input.files?.[0] ?? null; }

    async uploadSpriteImage(): Promise<void> {
        if (!this.selectedFile) {
            this.error.set("Select a PNG file before uploading.");
            return;
        }

        await this.run(async () => {
            const v = this.spriteUploadForm.getRawValue();
            this.mutationResult.set(await firstValueFrom(this.spritesService.replaceSpriteImage(v.id, this.selectedFile!)));
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
