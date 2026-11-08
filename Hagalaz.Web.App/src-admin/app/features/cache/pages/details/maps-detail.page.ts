import { Component, computed, DestroyRef, effect, inject, linkedSignal, signal, untracked } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { MatButtonModule } from "@angular/material/button";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import { MatCardModule } from "@angular/material/card";
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MatDialog } from "@angular/material/dialog";
import { MatDividerModule } from "@angular/material/divider";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatMenuModule } from "@angular/material/menu";
import { MatSelectModule } from "@angular/material/select";
import { MatSnackBar } from "@angular/material/snack-bar";
import { MatTabsModule } from "@angular/material/tabs";
import { MatTooltipModule } from "@angular/material/tooltip";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { firstValueFrom } from "rxjs";
import type { PlacementGizmoMode, PlacementGizmoResult } from "../../../../../../shared/model-viewer/placement-gizmo";
import { PortalBreadcrumbDirective } from "../../../../layout/portal-breadcrumb.directive";
import { CachePendingChangesService } from "../../cache-pending-changes.service";
import { CacheObjectsService, type ObjectCatalogEntry } from "../../services/cache-objects.service";
import { mapAppearanceKey, selectedMapIds, type MapFloor, type MapPlacement, type MapWorldMapRefreshResult } from "../../services/map-render.models";
import { CacheMapSceneStore } from "../../stores/cache-map-scene.store";
import { injectCacheDefinitionId } from "../cache-definition-id";
import { CacheWriteDialogComponent } from "../cache-write-dialog";
import { buildMapScene, mapLandscapeSaves, MapScenePreparation, unstoredEditNotes, type MapObjectRange, type MapObjectTransform, type MapSceneDraft, type MapSceneFilter } from "../map-scene-geometry";
import { regionDetails, sceneGroups, scenePlacements, shapeLabel, terrainDetails, type SceneSelection } from "../map-scene-tree";
import { MapAssetPanelComponent } from "../map-asset-panel.component";
import { MapLoadStatusComponent } from "../map-load-status.component";
import { MapRegionDetailsComponent } from "../map-region-details.component";
import { MapScenePanelComponent } from "../map-scene-panel.component";
import { applyBrushStroke, applyTerrainEdits, floorKey, mapTerrainSaves, paintedFloorTextures, terrainBrushOutline, terrainEditNotes, terrainStrokePreview, type BrushPoint, type TerrainBrush } from "../map-terrain-edit";
import { MapTerrainPanelComponent } from "../map-terrain-panel.component";
import { MapViewportComponent } from "../map-viewport.component";

@Component({
    selector: "cache-maps-detail-page",
    imports: [PortalBreadcrumbDirective, MapViewportComponent, MapLoadStatusComponent, MapScenePanelComponent, MapAssetPanelComponent, MapRegionDetailsComponent, MapTerrainPanelComponent, MatButtonModule, MatButtonToggleModule, MatCardModule, MatCheckboxModule, MatDividerModule, MatFormFieldModule, MatIconModule, MatInputModule, MatMenuModule, MatSelectModule, MatTabsModule, MatTooltipModule, RouterLink],
    providers: [CacheMapSceneStore],
    templateUrl: "./maps-detail.page.html",
    styleUrl: "./maps-detail.page.scss",
    host: { class: "min-w-0" },
})
export class MapsDetailPageComponent {
    readonly store = inject(CacheMapSceneStore);
    readonly id = injectCacheDefinitionId();
    private readonly route = inject(ActivatedRoute);
    private readonly params = toSignal(this.route.queryParamMap, { initialValue: this.route.snapshot.queryParamMap });
    readonly selection = computed(() => ({ id: this.id() ?? NaN, width: dimension(this.params().get("width")), height: dimension(this.params().get("height")) }));
    readonly selectedIds = computed(() => selectedMapIds(this.selection()));
    readonly selectionKey = computed(() => `${this.selection().id}:${this.selection().width}:${this.selection().height}`);
    readonly planes = signal([true, false, false, false]);
    readonly landscape = signal(true);
    readonly underwater = signal(true);
    readonly waterSurface = signal(true);
    readonly terrainVisible = signal(true);
    readonly hiddenGroups = signal<ReadonlySet<string>>(new Set());
    readonly hiddenPlacements = linkedSignal<ReadonlySet<MapPlacement>>(() => { this.selectionKey(); return new Set(); });
    private readonly sceneFilter = computed<MapSceneFilter>(() => ({
        terrain: this.terrainVisible(),
        waterSurface: this.waterSurface(),
        hiddenShapes: new Set(sceneGroups.filter(group => this.hiddenGroups().has(group.id)).flatMap(group => group.shapes)),
        hiddenPlacements: this.hiddenPlacements(),
    }));
    private readonly visiblePlanes = computed(() => [...this.planes(), this.underwater()]);
    private readonly preparation = linkedSignal(() => { this.selectionKey(); return new MapScenePreparation(); });
    readonly useRegionAtmosphere = signal(true);
    readonly selectedObject = signal<MapObjectRange | null>(null);
    readonly sceneSelection = linkedSignal<SceneSelection | null>(() => { this.selectionKey(); return null; });
    readonly selectedSceneDetails = computed(() => this.store.regions().map(this.sceneSelection() === "terrain" ? terrainDetails : regionDetails));
    readonly editTool = signal<"select" | "move" | "rotate" | "scale" | "terrain">("select");
    readonly editTools = [
        { id: "select", label: "Select", icon: "near_me" },
        { id: "move", label: "Move", icon: "open_with" },
        { id: "rotate", label: "Rotate", icon: "rotate_right" },
        { id: "scale", label: "Scale", icon: "fit_screen" },
        { id: "terrain", label: "Terrain", icon: "landscape" },
    ] as const;
    readonly gizmoMode = computed(() => gizmoModes[this.editTool()] ?? null);
    readonly snap = signal(1);
    readonly edits = linkedSignal<MapSceneDraft | undefined>(() => { this.selectionKey(); return undefined; });
    private readonly editHistory = linkedSignal<readonly (MapSceneDraft | undefined)[]>(() => { this.selectionKey(); return []; });
    readonly canUndo = computed(() => this.editHistory().length > 0);
    private readonly terrainEdits = computed(() => this.edits()?.terrain);
    private readonly floorCatalog = computed(() => new Map(this.store.floors().map(floor => [floorKey(floor.overlay, floor.id), floor])));
    /** What the scene draws; the store keeps the regions as stored. */
    private readonly previewRegions = computed(() => applyTerrainEdits(this.store.regions(), this.terrainEdits(), this.floorCatalog()));
    readonly brush = signal<TerrainBrush>({ tool: "raise", plane: 0, size: 3, strength: 2, underlay: null, overlay: null, overlayShape: 0, overlayRotation: 0 });
    readonly brushPoint = signal<BrushPoint | null>(null);
    readonly strokePoints = signal<readonly BrushPoint[] | null>(null);
    readonly strokePreview = computed(() => {
        const points = this.strokePoints();
        return points && terrainStrokePreview(this.previewRegions(), this.brush(), points, this.floorCatalog());
    });
    readonly brushOutline = computed(() => {
        const point = this.brushPoint();
        return point && terrainBrushOutline(this.previewRegions(), this.brush(), point);
    });
    // Terrain first, so a region's two saves stay together and in a fixed order.
    private readonly regionSaves = computed(() => [...mapTerrainSaves(this.store.regions(), this.terrainEdits()), ...mapLandscapeSaves(this.store.regions(), this.edits())].sort((left, right) => left.regionId - right.regionId));
    readonly canSave = computed(() => this.regionSaves().length > 0 && !this.store.saving());
    readonly canRebuildWorldMap = computed(() => this.store.regions().length > 0 && !this.store.worldMapRefreshing() && !this.store.saving());
    private readonly pendingChanges = inject(CachePendingChangesService);
    private readonly dialog = inject(MatDialog);
    private readonly snackBar = inject(MatSnackBar);
    private readonly objects = inject(CacheObjectsService);
    readonly selectedAsset = signal<ObjectCatalogEntry | null>(null);
    private readonly pendingSelection = linkedSignal<MapPlacement | null>(() => { this.selectionKey(); return null; });
    readonly positionMode = signal<"tile" | "world">("tile");
    readonly shapeLabel = shapeLabel;
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
    private readonly selectedObjectId = computed(() => this.selectedObject()?.placement.id ?? null);
    readonly objectDefinition = this.objects.definitionResource(this.selectedObjectId);
    readonly objectProperties = computed(() => {
        const definition = this.objectDefinition.hasValue() ? this.objectDefinition.value() : undefined;
        const selected = this.selectedObject();
        if (!definition || !selected) return null;
        const { x, y, plane } = selected.placement;
        const flags = this.store.regions().find(region => region.id === selected.regionId)?.planes[plane]?.flags[x * 64 + y] ?? 0;
        return [
            { label: "Interactive", value: definition.actions.some(Boolean) },
            { label: "Impassable", value: definition.clipType !== 0 },
            { label: "Solid", value: definition.solid },
            { label: "Gateway", value: definition.gateway },
            { label: "Bridge tile", value: (flags & 2) !== 0 },
        ];
    });
    /** Tile coordinates are region-local; world mode shows and edits them relative to the whole map. */
    readonly positionBase = computed(() => {
        const regionId = this.selectedObject()?.regionId;
        return this.positionMode() === "world" && regionId !== undefined ? { x: (regionId >> 8) * 64, y: (regionId & 255) * 64 } : { x: 0, y: 0 };
    });
    readonly regionAtmosphere = computed(() => this.store.regions().find(region => region.id === this.selection().id)?.atmosphere ?? null);
    readonly scene = computed(() => buildMapScene(
        this.previewRegions(), this.store.appearances(), this.store.textures(), this.visiblePlanes(), this.landscape(), this.preparation(),
        this.useRegionAtmosphere() ? this.selection().id : null, this.edits(), this.sceneFilter(),
    ));
    readonly objectNames = computed(() => {
        const names = new Map<number, string>();
        for (const { id, name } of this.store.appearances().values()) {
            const label = name?.trim();
            if (label && label.toLowerCase() !== "null") names.set(id, label);
        }
        return names;
    });
    readonly sourcePlacements = computed(() => {
        const draft = this.edits();
        if (!this.landscape()) return [];
        return scenePlacements(this.store.regions(), this.visiblePlanes(), draft?.additions ?? [], placement => draft?.transforms.get(placement) === null);
    });
    readonly canAddObject = computed(() => this.selectedAsset()?.shape != null && this.store.regions().length > 0);
    readonly writeActivity = computed(() => this.store.saving() ? "Saving changes" : this.store.worldMapRefreshing() ? "Updating world map" : null);
    // Edited rotations and additions need their own appearances, independent of source plane visibility.
    // The loader only reacts when the set of appearance keys changes, not on every move or scale step.
    private readonly previewPlacements = computed(() => {
        const draft = this.edits();
        return draft && [...draft.additions.map(item => item.placement), ...draft.transforms.keys()].flatMap(placement => {
            const transform = draft.transforms.get(placement);
            return transform === null ? [] : [transform ? { ...placement, rotation: transform.rotation } : placement];
        });
    }, { equal: (left, right) => appearanceKeys(left) === appearanceKeys(right) });
    /** Includes the floor on the brush, so its texture is ready before the first stroke. */
    private readonly previewTextures = computed(() => {
        const painted = paintedFloorTextures(this.terrainEdits(), this.floorCatalog());
        const texture = this.editTool() === "terrain" ? brushFloorTexture(this.brush(), this.floorCatalog()) : -1;
        return texture >= 0 && !painted.includes(texture) ? [...painted, texture] : painted;
    }, { equal: (left, right) => left.join() === right.join() });

    constructor() {
        this.pendingChanges.register({ dirty: () => this.edits() !== undefined, saving: () => this.store.saving() || this.store.worldMapRefreshing() }, inject(DestroyRef));
        effect(() => {
            const value = { planes: this.visiblePlanes(), landscape: this.landscape(), previewPlacements: this.previewPlacements(), previewTextures: this.previewTextures() };
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
    selectScene(selection: SceneSelection): void {
        this.pendingSelection.set(null);
        this.selectedObject.set(null);
        this.sceneSelection.set(selection);
        if (this.editTool() === "terrain") this.editTool.set("select");
    }
    selectObject(range: MapObjectRange | null): void {
        this.sceneSelection.set(null);
        this.selectedObject.set(range);
    }
    chooseTool(tool: "select" | "move" | "rotate" | "scale" | "terrain"): void {
        this.editTool.set(tool);
        if (tool === "terrain") void this.store.loadFloors();
    }
    changeBrush(brush: TerrainBrush): void {
        this.brush.set(brush);
        if (!this.planes()[brush.plane]) this.togglePlane(brush.plane, true);
    }
    applyStroke(points: readonly BrushPoint[]): void {
        const terrain = applyBrushStroke(this.previewRegions(), this.terrainEdits(), this.brush(), points);
        if (terrain !== this.terrainEdits()) this.commitEdit(draft => ({ transforms: draft?.transforms ?? new Map(), additions: draft?.additions ?? [], terrain }));
        // Goes in the same frame as the finished ground, so there is no gap between them.
        this.strokePoints.set(null);
    }
    selectPlacement(placement: MapPlacement): void {
        const range = this.scene().geometry?.placements.find(candidate => candidate.placement === placement);
        if (range) this.selectObject(range);
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
    changeTransform(field: keyof MapObjectTransform, value: number): void {
        const selected = this.selectedObject(), current = this.objectTransform();
        if (!selected || !current || !Number.isFinite(value)) return;
        const { min, max, step } = transformLimits(field, this.snap());
        const next = Math.min(max, Math.max(min, Math.round(value / step) * step));
        if (field === "plane" && !this.planes()[next]) this.togglePlane(next, true);
        if (next !== current[field]) this.setObjectDraft(selected, { ...current, [field]: next });
    }
    applyObjectGizmo({ x, y, plane, turns, scale }: PlacementGizmoResult): void {
        const selected = this.selectedObject(), current = this.objectTransform();
        if (!selected || !current) return;
        if (plane !== current.plane && !this.planes()[plane]) this.togglePlane(plane, true);
        this.setObjectDraft(selected, { ...current, x, y, plane, rotation: (current.rotation + turns) % 4, scale });
    }
    private setObjectDraft(selected: MapObjectRange, transform: MapObjectTransform | null): void {
        this.commitEdit(draft => {
            const transforms = new Map(draft?.transforms);
            transforms.set(selected.placement, transform);
            return { ...draft, transforms, additions: draft?.additions ?? [] };
        });
    }
    private commitEdit(change: (draft: MapSceneDraft | undefined) => MapSceneDraft): void {
        const draft = this.edits();
        this.editHistory.update(history => [...history, draft]);
        this.edits.set(change(draft));
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
        this.commitEdit(draft => ({
            ...draft,
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
        this.editHistory.set([]);
        this.edits.set(undefined);
    }
    async saveChanges(): Promise<void> {
        const saves = this.regionSaves();
        const regionIds = [...new Set(saves.map(save => save.regionId))];
        if (!saves.length || !(await this.confirmSave(regionIds))) return;
        if (!(await this.store.saveRegions(saves))) return;
        this.reload();
        const refresh = await this.store.refreshWorldMap(regionIds);
        this.notify(`Saved ${regionList(regionIds)} to the cache.${refresh ? ` ${describeWorldMapRefresh(refresh)}` : ""}`);
    }
    async rebuildWorldMap(): Promise<void> {
        const regionIds = this.store.regions().map(region => region.id);
        if (!(await this.confirmWrite(`Rebuild the world map for ${regionList(regionIds)}? ${otherAreasNote}`))) return;
        const refresh = await this.store.refreshWorldMap(regionIds);
        if (refresh) this.notify(describeWorldMapRefresh(refresh));
    }
    private notify(message: string): void {
        this.snackBar.open(message, undefined, { duration: 4000 });
    }
    private async confirmSave(regionIds: readonly number[]): Promise<boolean> {
        return this.confirmWrite([`Save placement changes to ${regionIds.length === 1 ? "region" : "regions"} ${regionIds.join(", ")}?`, ...unstoredEditNotes(this.edits()), ...terrainEditNotes(this.terrainEdits()), otherAreasNote].join(" "));
    }
    private async confirmWrite(data: string): Promise<boolean> {
        return (await firstValueFrom(this.dialog.open(CacheWriteDialogComponent, { data, maxWidth: "32rem" }).afterClosed())) === true;
    }
    undo(): void {
        const history = this.editHistory();
        if (!history.length) return;
        const selected = this.selectedObject()?.placement;
        this.pendingSelection.set(null);
        this.editHistory.set(history.slice(0, -1));
        this.edits.set(history[history.length - 1]);
        // Vertex ranges shift with each draft, and an undone addition no longer exists.
        this.selectedObject.set(this.scene().geometry?.placements.find(candidate => candidate.placement === selected) ?? null);
    }
    async retryLoad(): Promise<void> {
        if (await this.pendingChanges.canLeave()) this.reload();
    }
    // A new load replaces any still running, so saved regions always come back from the cache.
    private reload(): void {
        this.resetEdits();
        this.store.load({ ...this.selection(), keys: {} });
    }
    togglePlane(plane: number, checked: boolean): void { this.planes.update(planes => planes.map((value, index) => index === plane ? checked : value)); }
}

const gizmoModes: Partial<Record<"select" | "move" | "rotate" | "scale" | "terrain", PlacementGizmoMode>> = { move: "translate", rotate: "rotate", scale: "scale" };

function transformLimits(field: keyof MapObjectTransform, snap: number): { min: number; max: number; step: number } {
    if (field === "scale") return { min: 0.25, max: 4, step: 0.25 };
    return field === "x" || field === "y" ? { min: 0, max: 63, step: snap } : { min: 0, max: 3, step: 1 };
}

function brushFloorTexture(brush: TerrainBrush, floors: ReadonlyMap<string, MapFloor>): number {
    if (brush.tool === "underlay") return floors.get(floorKey(false, brush.underlay ?? 0))?.textureId ?? -1;
    return brush.tool === "overlay" ? floors.get(floorKey(true, brush.overlay ?? 0))?.textureId ?? -1 : -1;
}

const otherAreasNote = "World map areas other than the main map are not updated.";

function regionList(ids: readonly number[]): string {
    return ids.length === 1 ? `region ${ids[0]}` : `${ids.length} regions`;
}

function describeWorldMapRefresh({ refreshed, skipped }: MapWorldMapRefreshResult): string {
    const updated = refreshed.length ? `World map updated for ${regionList(refreshed)}.` : "World map already up to date.";
    return skipped.length ? `${updated} Not updated: ${skipped.map(skip => skip.id).join(", ")}.` : updated;
}

function dimension(value: string | null): number {
    return value === null ? 1 : /^\d+$/.test(value) ? Number(value) : NaN;
}

function appearanceKeys(placements: readonly MapPlacement[] | undefined): string {
    return placements ? [...new Set(placements.map(mapAppearanceKey))].sort().join() : "";
}
