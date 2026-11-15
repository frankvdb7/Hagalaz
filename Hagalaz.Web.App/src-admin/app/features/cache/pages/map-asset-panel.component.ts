import { DecimalPipe } from "@angular/common";
import { Component, computed, type ElementRef, inject, input, model, signal, viewChild } from "@angular/core";
import { toObservable, toSignal } from "@angular/core/rxjs-interop";
import { MatButtonModule } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatListModule } from "@angular/material/list";
import { MatSelectModule } from "@angular/material/select";
import { MatTooltipModule } from "@angular/material/tooltip";
import { debounceTime } from "rxjs";
import { CacheObjectsService, type ObjectCatalogEntry } from "../services/cache-objects.service";
import { sceneGroups } from "./map-scene-tree";
import { mapObjectDragType } from "./map-viewport.component";
import { ObjectModelThumbnailComponent } from "./object-model-thumbnail.component";

/** Searchable object catalog; objects can be dragged onto the viewport. */
@Component({
    selector: "cache-map-asset-panel",
    imports: [DecimalPipe, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatListModule, MatSelectModule, MatTooltipModule, ObjectModelThumbnailComponent],
    templateUrl: "./map-asset-panel.component.html",
    styleUrl: "./map-asset-panel.component.scss",
})
export class MapAssetPanelComponent {
    /** Catalog requests wait until a region has loaded. */
    readonly active = input(false);
    readonly selectedAsset = model<ObjectCatalogEntry | null>(null);
    readonly objectTypes = sceneGroups;
    /** A scene group ID, or empty for every type. */
    readonly objectType = signal("");
    readonly search = signal("");
    readonly listView = signal(true);
    readonly draggingId = signal<number | null>(null);
    private readonly dragPreview = viewChild.required<ElementRef<HTMLElement>>("dragPreview");
    private readonly dragPreviewImage = viewChild.required<ElementRef<HTMLCanvasElement>>("dragPreviewImage");
    private readonly dragPreviewName = viewChild.required<ElementRef<HTMLElement>>("dragPreviewName");
    readonly catalogOffset = signal(0);
    readonly catalogPageSize = 40;
    private readonly catalogQuery = toSignal(toObservable(this.search).pipe(debounceTime(250)), { initialValue: "" });
    private readonly shapes = computed(() => sceneGroups.find(group => group.id === this.objectType())?.shapes.join(","));
    readonly catalog = inject(CacheObjectsService).catalogResource(computed(() => {
        if (!this.active()) return null;
        const shapes = this.shapes();
        return { query: this.catalogQuery().trim(), offset: this.catalogOffset(), limit: this.catalogPageSize, ...(shapes ? { shapes } : {}) };
    }));
    readonly catalogPage = computed(() => this.catalog.hasValue() ? this.catalog.value() : undefined);
    readonly catalogItems = computed(() => this.catalogPage()?.items ?? []);
    readonly emptyMessage = computed(() => {
        if (this.catalogItems().length) return null;
        if (this.catalog.isLoading()) return "Loading objects…";
        return this.catalog.error() ? "Could not load the object catalog." : "No matching objects.";
    });

    changeType(type: string): void {
        this.objectType.set(type);
        this.catalogOffset.set(0);
    }

    searchAssets(query: string): void {
        this.search.set(query);
        this.catalogOffset.set(0);
    }

    dragObject(event: DragEvent, asset: ObjectCatalogEntry): void {
        if (asset.shape === null || !event.dataTransfer) return;
        this.selectedAsset.set(asset);
        event.dataTransfer.setData(mapObjectDragType, String(asset.id));
        event.dataTransfer.effectAllowed = "copy";
        event.dataTransfer.setDragImage(this.preparedDragPreview(event.currentTarget as HTMLElement, asset), 28, 28);
        this.draggingId.set(asset.id);
    }

    // The browser captures the drag image during dragstart, so it must be complete before this returns:
    // the loaded thumbnail is drawn directly, because a new image element would not have decoded in time.
    private preparedDragPreview(source: HTMLElement, asset: ObjectCatalogEntry): HTMLElement {
        const canvas = this.dragPreviewImage().nativeElement;
        const thumbnail = source.querySelector("img");
        const image = thumbnail?.complete && thumbnail.naturalWidth > 0 ? thumbnail : null;
        canvas.hidden = !image;
        if (image) {
            const scale = Math.min(canvas.width / image.naturalWidth, canvas.height / image.naturalHeight);
            const width = image.naturalWidth * scale, height = image.naturalHeight * scale;
            const context = canvas.getContext("2d");
            context?.clearRect(0, 0, canvas.width, canvas.height);
            context?.drawImage(image, (canvas.width - width) / 2, (canvas.height - height) / 2, width, height);
        }
        this.dragPreviewName().nativeElement.textContent = this.name(asset);
        return this.dragPreview().nativeElement;
    }

    isSelected(asset: ObjectCatalogEntry): boolean {
        return this.selectedAsset()?.id === asset.id;
    }

    name(asset: ObjectCatalogEntry): string {
        return asset.name.trim() && asset.name.toLowerCase() !== "null" ? asset.name : "Unnamed object";
    }

    hint(asset: ObjectCatalogEntry): string {
        return asset.shape === null ? "No placeable model in this definition" : "Drag onto the map to place";
    }
}
