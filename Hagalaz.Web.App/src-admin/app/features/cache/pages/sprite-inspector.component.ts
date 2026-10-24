import { Component, computed, DestroyRef, effect, inject, input, model, signal, untracked } from "@angular/core";

import { MatButtonModule } from "@angular/material/button";

import { MatDialog, type MatDialogRef } from "@angular/material/dialog";

import { MatIconModule } from "@angular/material/icon";

import { MatTabsModule } from "@angular/material/tabs";

import { take } from "rxjs";

import { CachePendingChangesService } from "../cache-pending-changes.service";
import type { SpriteMetadataDto } from "../services/cache.models";

import { CacheSpritesStore } from "../stores/cache-sprites.store";

import { CacheWriteDialogComponent } from "./cache-write-dialog";

@Component({
    selector: "cache-sprite-inspector",

    imports: [MatButtonModule, MatIconModule, MatTabsModule],

    templateUrl: "./sprite-inspector.component.html",

    styleUrl: "./sprite-inspector.component.scss",
})
export class SpriteInspectorComponent {
    readonly id = input.required<number | null>();

    readonly metadata = input.required<SpriteMetadataDto | null>();

    readonly selectedFrame = input.required<number | null>();

    readonly currentImageUrl = input.required<string | null>();

    readonly tab = model(0);

    readonly images = inject(CacheSpritesStore);

    private readonly dialog = inject(MatDialog);
    private readonly pendingChanges = inject(CachePendingChangesService);

    private confirmationDialog: MatDialogRef<CacheWriteDialogComponent> | null = null;

    readonly selectedFile = signal<File | null>(null);

    readonly candidate = signal<{ url: string; width: number; height: number } | null>(null);

    readonly fileError = signal<string | null>(null);

    readonly validating = signal(false);

    readonly confirming = signal(false);

    readonly confirmation = computed(() => (this.images.mutationResult()?.id === this.id() ? this.images.mutationResult() : null));
    readonly mutationError = computed(() => (this.images.mutationId() === this.id() ? this.images.mutationError() : null));

    readonly chooseDisabled = computed(() => this.images.saving() || this.confirming());

    readonly replaceDisabled = computed(() => !this.candidate() || this.validating() || this.chooseDisabled());

    readonly fileName = computed(() => this.selectedFile()?.name ?? "No file selected");

    readonly replaceLabel = computed(() => (this.images.saving() ? "Replacing sprite..." : "Replace entire sprite"));

    readonly frameLabel = computed(() => this.selectedFrame() ?? "None");

    constructor() {
        this.pendingChanges.register({ dirty: () => this.selectedFile() !== null, saving: () => this.images.saving() }, inject(DestroyRef));
        inject(DestroyRef).onDestroy(() => this.confirmationDialog?.close(false));

        effect(() => {
            this.id();

            untracked(() => {
                this.selectedFile.set(null);

                this.confirmationDialog?.close(false);
            });
        });

        effect((onCleanup) => {
            const file = this.selectedFile();

            this.candidate.set(null);

            this.fileError.set(null);

            this.validating.set(false);

            if (!file) return;

            if (file.type !== "image/png") {
                this.fileError.set("Choose a PNG image.");

                return;
            }

            const url = URL.createObjectURL(file);

            const image = new Image();

            this.validating.set(true);

            image.onload = () => {
                this.validating.set(false);

                if (image.naturalWidth > 0 && image.naturalHeight > 0) this.candidate.set({ url, width: image.naturalWidth, height: image.naturalHeight });
                else this.fileError.set("This PNG has no usable image dimensions.");
            };

            image.onerror = () => {
                this.validating.set(false);

                this.fileError.set("This file could not be decoded. Choose a valid PNG image.");
            };

            image.src = url;

            onCleanup(() => {
                image.onload = null;

                image.onerror = null;

                URL.revokeObjectURL(url);
            });
        });

        effect(() => {
            const result = this.images.mutationResult();

            if (result)
                untracked(() => {
                    if (result.id === this.id()) this.selectedFile.set(null);
                });
        });
    }

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;

        this.selectedFile.set(input.files?.[0] ?? null);

        input.value = "";

        this.images.clearError();
    }

    replaceSprite(): void {
        const id = this.id();

        const file = this.selectedFile();

        if (id === null || !file || !this.candidate() || this.validating() || this.images.saving() || this.confirming()) return;

        this.confirming.set(true);

        this.confirmationDialog = this.dialog.open(CacheWriteDialogComponent, {
            data: `Replace the entire sprite #${id} with ${file.name}? All existing frames will be replaced, not only the selected frame.`,

            maxWidth: "32rem",
        });

        this.confirmationDialog

            .afterClosed()

            .pipe(take(1))

            .subscribe((confirmed) => {
                this.confirming.set(false);

                if (confirmed === true && id === this.id() && file === this.selectedFile()) this.images.uploadSpriteImage({ id, file });
            });
    }
}
