import { CdkVirtualScrollViewport, ScrollingModule } from "@angular/cdk/scrolling";
import { Component, computed, effect, inject, linkedSignal, signal, untracked, viewChild } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatTooltipModule } from "@angular/material/tooltip";
import { PortalBreadcrumbDirective } from "../../../../layout/portal-breadcrumb.directive";
import type { SpriteMetadataDto } from "../../services/cache.models";
import { CacheSpritesService } from "../../services/cache-sprites.service";
import { CacheSpriteDefinitionsStore } from "../../stores/cache-sprite-definitions.store";
import { CacheSpritesStore } from "../../stores/cache-sprites.store";
import { injectCacheDefinitionId } from "../cache-definition-id";
import { DefinitionPickerComponent } from "../definition-picker.component";
import { SpriteFrameThumbnailComponent } from "../sprite-frame-thumbnail.component";
import { SpriteInspectorComponent } from "../sprite-inspector.component";

@Component({
    selector: "cache-sprites-detail-page",
    imports: [
        ScrollingModule,
        DefinitionPickerComponent,
        PortalBreadcrumbDirective,
        MatButtonModule,
        MatButtonToggleModule,
        MatCardModule,
        MatIconModule,
        MatProgressBarModule,
        SpriteInspectorComponent,
        MatTooltipModule,
        SpriteFrameThumbnailComponent,
    ],
    providers: [CacheSpritesService, CacheSpriteDefinitionsStore, CacheSpritesStore],
    templateUrl: "./sprites-detail.page.html",
    styleUrl: "./sprites-detail.page.scss",
})
export class SpriteDefinitionsDetailPageComponent {
    readonly store = inject(CacheSpriteDefinitionsStore);
    readonly images = inject(CacheSpritesStore);
    readonly viewport = viewChild(CdkVirtualScrollViewport);
    readonly id = injectCacheDefinitionId();
    readonly metadata = computed(() => {
        const value = this.store.byIdResult() as SpriteMetadataDto | null;
        return value?.id === this.id() ? value : null;
    });
    readonly frames = computed(() => Array.from({ length: this.metadata()?.frameCount ?? 0 }, (_, index) => index));
    readonly selectedFrame = linkedSignal<{ id: number | null; count: number }, number | null>({
        source: () => ({ id: this.id(), count: this.frames().length }),
        computation: (source, previous) =>
            source.count === 0 ? null : Math.min(previous?.source.id === source.id ? (previous.value ?? 0) : 0, source.count - 1),
    });
    readonly background = signal("checkerboard");
    readonly zoom = signal(100);
    readonly fit = signal(true);
    readonly inspectorTab = signal(0);
    readonly imageUrl = signal<string | null>(null);
    readonly currentImageUrl = computed(() => (this.images.imageId() === this.id() ? this.imageUrl() : null));
    readonly canDownload = computed(() => !!this.currentImageUrl() && this.images.imageFrame() === this.selectedFrame() && !this.images.previewLoading());

    readonly displayId = computed(() => this.id() ?? "Invalid ID");
    readonly activeFrameId = computed(() => (this.selectedFrame() === null ? null : `sprite-frame-${this.selectedFrame()}`));
    readonly zoomLabel = computed(() => (this.fit() ? "Fit" : `${this.zoom()}%`));
    readonly zoomOutDisabled = computed(() => !this.currentImageUrl() || (!this.fit() && this.zoom() <= 25));
    readonly zoomInDisabled = computed(() => !this.currentImageUrl() || (!this.fit() && this.zoom() >= 1600));
    readonly previewWidth = computed(() => (this.fit() ? null : ((this.metadata()?.width ?? 0) * this.zoom()) / 100));
    readonly previewEmpty = computed(() => !this.store.loading() && this.frames().length === 0);
    readonly previewDescription = computed(() => (this.currentImageUrl() ? this.images.imageDescription() : "No frame loaded"));

    constructor() {
        effect(() => {
            const id = this.id();
            untracked(() => {
                this.images.previewSpriteImage(null);
                if (id !== null) this.store.loadById(id);
            });
        });
        effect(() => {
            const metadata = this.metadata();
            const frame = this.selectedFrame();
            if (metadata && !this.store.loading()) {
                untracked(() => this.images.previewSpriteImage(frame === null ? null : { id: metadata.id, frame }));
            }
        });
        effect((onCleanup) => {
            const blob = this.images.imageBlob();
            this.imageUrl.set(null);
            if (!blob) return;
            const url = URL.createObjectURL(blob);
            this.imageUrl.set(url);
            onCleanup(() => URL.revokeObjectURL(url));
        });
        effect(() => {
            const result = this.images.mutationResult();
            if (!result) return;
            untracked(() => {
                if (result.id !== this.id()) return;
                this.store.loadById(result.id);
            });
        });
    }

    selectFrame(frame: number): void {
        if (!Number.isInteger(frame) || frame < 0 || frame >= this.frames().length) return;
        this.selectedFrame.set(frame);
        this.viewport()?.scrollToIndex(frame);
    }

    onFrameKey(event: KeyboardEvent): void {
        const current = this.selectedFrame() ?? 0;
        const targets: Record<string, number> = { ArrowDown: current + 1, ArrowUp: current - 1, Home: 0, End: this.frames().length - 1 };
        if (!(event.key in targets)) return;
        event.preventDefault();
        this.selectFrame(targets[event.key]);
    }

    changeZoom(factor: number): void {
        this.fit.set(false);
        this.zoom.update((zoom) => Math.max(25, Math.min(1600, zoom * factor)));
    }

    retryPreview(): void {
        const id = this.id();
        const frame = this.selectedFrame();
        if (id !== null && frame !== null) this.images.previewSpriteImage({ id, frame });
    }

    reload(): void {
        const id = this.id();
        if (id !== null) this.store.loadById(id);
    }

    downloadFrame(): void {
        const url = this.currentImageUrl();
        if (!url || !this.canDownload()) return;
        const link = document.createElement("a");
        link.href = url;
        link.download = `sprite-${this.id()}-frame-${this.selectedFrame()}.png`;
        link.click();
    }
}
