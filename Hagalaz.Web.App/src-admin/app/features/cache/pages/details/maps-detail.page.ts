import { A11yModule } from "@angular/cdk/a11y";
import { OverlayModule } from "@angular/cdk/overlay";
import { DecimalPipe, NgTemplateOutlet } from "@angular/common";
import { Component, computed, effect, inject, linkedSignal, signal, untracked } from "@angular/core";
import { toObservable, toSignal } from "@angular/core/rxjs-interop";
import { FormField, form, min, max } from "@angular/forms/signals";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatSelectModule } from "@angular/material/select";
import { MatTabsModule } from "@angular/material/tabs";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { debounceTime } from "rxjs";
import { PortalBreadcrumbDirective } from "../../../../layout/portal-breadcrumb.directive";
import { CacheObjectsService, type ObjectCatalogEntry } from "../../services/cache-objects.service";
import { mapAppearanceKey, selectedMapIds, type MapPlacement } from "../../services/map-render.models";
import { CacheMapSceneStore } from "../../stores/cache-map-scene.store";
import { injectCacheDefinitionId } from "../cache-definition-id";
import { buildMapScene, MapScenePreparation, type MapObjectRange, type MapObjectTransform, type MapSceneDraft } from "../map-scene-geometry";
import { MapViewportComponent, mapObjectDragType } from "../map-viewport.component";
import { ObjectModelThumbnailComponent } from "../object-model-thumbnail.component";

@Component({
    selector: "cache-maps-detail-page",
    imports: [A11yModule, OverlayModule, DecimalPipe, NgTemplateOutlet, FormField, PortalBreadcrumbDirective, MapViewportComponent, ObjectModelThumbnailComponent, MatButtonModule, MatCardModule, MatCheckboxModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressBarModule, MatSelectModule, MatTabsModule, RouterLink],
    providers: [CacheMapSceneStore],
    templateUrl: "./maps-detail.page.html",
    styleUrl: "./maps-detail.page.scss",
    host: { class: "min-w-0" },
})
export class MapsDetailPageComponent {
    readonly store = inject(CacheMapSceneStore);
    readonly id = injectCacheDefinitionId();
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly params = toSignal(this.route.queryParamMap, { initialValue: this.route.snapshot.queryParamMap });
    readonly selection = computed(() => ({ id: this.id() ?? NaN, width: dimension(this.params().get("width")), height: dimension(this.params().get("height")) }));
    readonly selectedIds = computed(() => selectedMapIds(this.selection()));
    readonly selectionKey = computed(() => `${this.selection().id}:${this.selection().width}:${this.selection().height}`);
    private readonly draft = linkedSignal(() => this.selection());
    readonly dimensions = form(this.draft, path => {
        min(path.id, 0); max(path.id, 65535);
        min(path.width, 1); max(path.width, 3);
        min(path.height, 1); max(path.height, 3);
    });
    readonly regionPanel = signal<"region" | "settings" | null>(null);
    readonly error = signal<string | null>(null);
    readonly planes = signal([true, false, false, false]);
    readonly landscape = signal(true);
    readonly underwater = signal(true);
    readonly waterSurface = signal(true);
    private readonly visiblePlanes = computed(() => [...this.planes(), this.underwater()]);
    readonly selectedPlane = computed(() => this.planes().filter(Boolean).length === 1 ? this.planes().indexOf(true) : -1);
    private readonly preparation = linkedSignal(() => { this.selectionKey(); return new MapScenePreparation(); });
    readonly useRegionAtmosphere = signal(true);
    readonly selectedObject = signal<MapObjectRange | null>(null);
    readonly inspectorTab = signal(0);
    readonly editTool = signal<"select" | "move" | "rotate" | "scale">("select");
    readonly editTools = [
        { id: "select", label: "Select", icon: "near_me" },
        { id: "move", label: "Move", icon: "open_with" },
        { id: "rotate", label: "Rotate", icon: "rotate_right" },
        { id: "scale", label: "Scale", icon: "zoom_out_map" },
    ] as const;
    readonly snap = signal(1);
    readonly edits = linkedSignal<MapSceneDraft | undefined>(() => { this.selectionKey(); return undefined; });
    readonly assetKind = signal<"objects" | "models" | "textures">("objects");
    readonly assetTabs = [
        { kind: "objects", label: "Objects", icon: "view_in_ar" },
        { kind: "models", label: "Models", icon: "layers" },
        { kind: "textures", label: "Textures", icon: "texture" },
    ] as const;
    readonly assetTab = computed(() => this.assetTabs.findIndex(tab => tab.kind === this.assetKind()));
    readonly assetSearch = signal("");
    readonly catalogOffset = signal(0);
    readonly catalogPageSize = 40;
    private readonly catalogQuery = toSignal(toObservable(this.assetSearch).pipe(debounceTime(250)), { initialValue: "" });
    readonly catalog = inject(CacheObjectsService).catalogResource(computed(() => this.assetKind() === "objects" && this.store.regions().length
        ? { query: this.catalogQuery().trim(), offset: this.catalogOffset(), limit: this.catalogPageSize } : null));
    readonly catalogPage = computed(() => this.catalog.hasValue() ? this.catalog.value() : undefined);
    readonly selectedAsset = signal<ObjectCatalogEntry | null>(null);
    readonly placementLimit = signal(50);
    readonly placementSearch = signal("");
    private readonly pendingSelection = linkedSignal<MapPlacement | null>(() => { this.selectionKey(); return null; });
    readonly assetList = signal(false);
    readonly assetLimit = signal(60);
    readonly assetId = signal<number | null>(null);
    readonly objectTransform = computed(() => {
        const placement = this.selectedObject()?.placement;
        return placement ? this.edits()?.transforms.get(placement) ?? { ...placement, scale: 1 } : null;
    });
    readonly objectModels = computed(() => {
        const placement = this.selectedObject()?.placement;
        if (!placement) return [];
        // Match the scene: the edited rotation's appearance once loaded, otherwise the source appearance.
        const appearances = this.store.appearances();
        const rotation = this.objectTransform()?.rotation ?? placement.rotation;
        return (appearances.get(mapAppearanceKey({ ...placement, rotation })) ?? appearances.get(mapAppearanceKey(placement)))?.parts ?? [];
    });
    readonly regionAtmosphere = computed(() => this.store.regions().find(region => region.id === this.selection().id)?.atmosphere ?? null);
    readonly scene = computed(() => buildMapScene(
        this.store.regions(), this.store.appearances(), this.store.textures(), this.visiblePlanes(), this.landscape(), this.preparation(),
        this.useRegionAtmosphere() ? this.selection().id : null, this.edits(), this.waterSurface(),
    ));
    readonly objectNames = computed(() => {
        const names = new Map<number, string>();
        for (const { id, name } of this.store.appearances().values()) {
            const label = name?.trim();
            if (label && label.toLowerCase() !== "null") names.set(id, label);
        }
        return names;
    });
    readonly scenePlacements = computed(() => {
        const query = this.placementSearch().trim().toLowerCase();
        const ranges = this.scene().geometry?.placements ?? [];
        const names = this.objectNames();
        return query ? ranges.filter(range => String(range.placement.id).includes(query) || names.get(range.placement.id)?.toLowerCase().includes(query)) : ranges;
    });
    readonly visibleScenePlacements = computed(() => this.scenePlacements().slice(0, this.placementLimit()));
    readonly assetItems = computed(() => {
        const query = this.assetSearch().trim().toLowerCase();
        const kind = this.assetKind();
        const ids = kind === "textures" ? [...this.store.textures().keys()]
            : kind === "models" ? [...this.store.appearances().values()].flatMap(appearance => appearance.parts.map(part => part.id))
            : [];
        return [...new Set(ids)].filter(id => String(id).includes(query)).sort((a, b) => a - b);
    });
    readonly visibleAssets = computed(() => this.assetItems().slice(0, this.assetLimit()));
    readonly canAddObject = computed(() => this.selectedAsset()?.shape != null && this.store.regions().length > 0);
    readonly loadingText = computed(() => {
        const progress = this.store.progress();
        const stage = this.store.regions().length ? "Loading visible scene assets" : "Loading terrain";
        return `${stage}. Regions ${progress.regionsSettled}/${progress.regionsTotal}. Appearances ${progress.appearancesSettled}/${progress.appearancesTotal}. Textures ${progress.texturesSettled}/${progress.texturesTotal}. ${progress.failures} failed reads.`;
    });
    readonly visibleTiles = computed(() => this.store.regions().reduce((count, region) =>
        count + region.planes.reduce((tiles, plane, index) => tiles + (this.visiblePlanes()[index] ? plane.flags.length : 0), 0), 0));
    readonly visiblePlacements = computed(() => this.scene().geometry?.placements.length ?? 0);
    // Edited rotations and additions need their own appearances, independent of source plane visibility.
    // The loader only reacts when the set of appearance keys changes, not on every move or scale step.
    private readonly previewPlacements = computed(() => {
        const draft = this.edits();
        return draft && [...draft.additions.map(item => item.placement), ...draft.transforms.keys()].flatMap(placement => {
            const transform = draft.transforms.get(placement);
            return transform === null ? [] : [transform ? { ...placement, rotation: transform.rotation } : placement];
        });
    }, { equal: (left, right) => appearanceKeys(left) === appearanceKeys(right) });

    constructor() {
        effect(() => {
            const value = { planes: this.visiblePlanes(), landscape: this.landscape(), previewPlacements: this.previewPlacements() };
            untracked(() => this.store.setVisibility(value));
        });
        effect(() => {
            const selection = this.selection();
            untracked(() => this.store.load({ ...selection, keys: {} }));
        });
        effect(() => {
            const pending = this.pendingSelection();
            if (!pending) return;
            const range = this.scene().geometry?.placements.find(candidate => candidate.placement === pending);
            if (!range) return;
            this.pendingSelection.set(null);
            this.selectObject(range);
        });
    }
    selectObject(range: MapObjectRange | null): void {
        this.selectedObject.set(range);
        if (range) {
            this.inspectorTab.set(0);
        }
    }
    chooseTool(tool: "select" | "move" | "rotate" | "scale"): void {
        this.editTool.set(tool);
        this.inspectorTab.set(0);
    }
    selectAsset(id: number): void {
        this.assetId.set(id);
    }
    selectCatalogObject(asset: ObjectCatalogEntry): void {
        this.selectedAsset.set(asset);
    }
    dragCatalogObject(event: DragEvent, asset: ObjectCatalogEntry): void {
        if (asset.shape === null || !event.dataTransfer) return;
        this.selectCatalogObject(asset);
        event.dataTransfer.setData(mapObjectDragType, String(asset.id));
        event.dataTransfer.effectAllowed = "copy";
    }
    placeDroppedObject(drop: { id: number; x: number; z: number }): void {
        const regions = this.store.regions();
        const asset = this.selectedAsset();
        if (!regions.length || asset?.id !== drop.id) return;
        const x = Math.min(...regions.map(region => region.regionX)) * 64 + Math.floor(drop.x / 512);
        const y = Math.min(...regions.map(region => region.regionY)) * 64 + Math.floor(drop.z / 512);
        const region = regions.find(candidate => candidate.regionX === (x >> 6) && candidate.regionY === (y >> 6));
        if (region) this.placeCatalogObject(asset, region.id, x & 63, y & 63);
    }
    changeAssetKind(kind: "objects" | "models" | "textures"): void {
        if (this.assetKind() === kind) return;
        this.assetKind.set(kind);
        this.assetId.set(null);
        this.searchAssets("");
    }
    searchAssets(query: string): void {
        this.assetSearch.set(query);
        this.assetLimit.set(60);
        this.catalogOffset.set(0);
    }
    changeTransform(field: keyof MapObjectTransform, value: number): void {
        const selected = this.selectedObject(), current = this.objectTransform();
        if (!selected || !current || !Number.isFinite(value)) return;
        const [min, max] = field === "scale" ? [0.25, 4] : field === "x" || field === "y" ? [0, 63] : [0, 3];
        const step = field === "scale" ? 0.25 : field === "x" || field === "y" ? this.snap() : 1;
        const next = Math.min(max, Math.max(min, Math.round(value / step) * step));
        if (field === "plane" && !this.planes()[next]) this.togglePlane(next, true);
        this.setObjectDraft(selected, { ...current, [field]: next });
    }
    private setObjectDraft(selected: MapObjectRange, transform: MapObjectTransform | null): void {
        this.edits.update(draft => {
            const transforms = new Map(draft?.transforms);
            transforms.set(selected.placement, transform);
            return { transforms, additions: draft?.additions ?? [] };
        });
    }
    addObject(): void {
        const asset = this.selectedAsset();
        const selected = this.selectedObject();
        const region = this.store.regions().find(candidate => candidate.id === (selected?.regionId ?? this.id()));
        if (!asset || !region) return;
        const transform = this.objectTransform();
        this.placeCatalogObject(asset, region.id, transform?.x ?? 32, transform?.y ?? 32);
    }
    private placeCatalogObject(asset: ObjectCatalogEntry, regionId: number, x: number, y: number): void {
        if (asset.shape === null) return;
        const plane = Math.max(0, this.planes().indexOf(true));
        const placement = { id: asset.id, shape: asset.shape, rotation: 0, x, y, plane };
        if (!this.planes()[plane]) this.togglePlane(plane, true);
        this.landscape.set(true);
        this.edits.update(draft => ({
            transforms: draft?.transforms ?? new Map(),
            additions: [...draft?.additions ?? [], { regionId, placement }],
        }));
        this.pendingSelection.set(placement);
    }
    removeObject(): void {
        const selected = this.selectedObject();
        if (!selected) return;
        this.setObjectDraft(selected, null);
        this.selectObject(null);
    }
    resetEdits(): void {
        this.pendingSelection.set(null);
        this.selectedObject.set(null);
        this.edits.set(undefined);
    }
    async apply(): Promise<void> {
        const selection = this.draft();
        const ids = selectedMapIds(selection);
        this.error.set(null);
        if (!ids.length) { this.error.set("Enter a valid region ID and dimensions from 1 to 3 without crossing region coordinate 255."); return; }
        this.regionPanel.set(null);
        const current = this.selection();
        if (selection.id === current.id && selection.width === current.width && selection.height === current.height) this.retry();
        else await this.router.navigate(["/cache/types/maps", selection.id], { queryParamsHandling: "merge", queryParams: { width: selection.width === 1 ? null : selection.width, height: selection.height === 1 ? null : selection.height } });
    }
    retry(): void {
        if (this.store.loading()) return;
        this.resetEdits();
        this.store.load({ ...this.selection(), keys: {} });
    }
    togglePlane(plane: number, checked: boolean): void { this.planes.update(planes => planes.map((value, index) => index === plane ? checked : value)); }
    isolatePlane(plane: number): void { this.planes.update(planes => planes.map((_, index) => index === plane)); }
}

function dimension(value: string | null): number {
    return value === null ? 1 : /^\d+$/.test(value) ? Number(value) : NaN;
}

function appearanceKeys(placements: readonly MapPlacement[] | undefined): string {
    return placements ? [...new Set(placements.map(mapAppearanceKey))].sort().join() : "";
}
