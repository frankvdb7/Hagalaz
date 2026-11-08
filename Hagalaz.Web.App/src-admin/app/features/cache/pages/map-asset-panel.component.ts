import { DecimalPipe } from "@angular/common";
import { Component, computed, inject, input, model, signal } from "@angular/core";
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
