import { Component, computed, effect, inject, linkedSignal, signal, untracked, viewChild } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { FormField, form } from "@angular/forms/signals";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatMenuModule } from "@angular/material/menu";
import { MatPaginator, MatPaginatorModule, type PageEvent } from "@angular/material/paginator";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTabsModule } from "@angular/material/tabs";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { cacheTypeLabels, cacheTypeNames, type TypeKind } from "../services/cache.models";
import { CacheSpritesService } from "../services/cache-sprites.service";
import { CacheArchivesStore } from "../stores/cache-archives.store";
import { cacheDefinitionStores } from "../stores/cache-definition-stores";
import { ItemSpriteThumbnailComponent } from "./item-sprite-thumbnail.component";
import { SpriteFrameThumbnailComponent } from "./sprite-frame-thumbnail.component";

@Component({
    selector: "types-manager-page",
    imports: [
        FormField,
        RouterLink,
        MatButtonModule,
        MatCardModule,
        MatIconModule,
        MatTabsModule,
        MatTableModule,
        MatFormFieldModule,
        MatInputModule,
        MatMenuModule,
        MatPaginatorModule,
        MatProgressBarModule,
        SpriteFrameThumbnailComponent,
        ItemSpriteThumbnailComponent,
    ],
    providers: [CacheSpritesService, CacheArchivesStore, ...Object.values(cacheDefinitionStores)],
    templateUrl: "./types-manager.page.html",
    styleUrl: "./types-manager.page.scss",
})
export class TypesManagerPageComponent {
    readonly archives = inject(CacheArchivesStore);
    private readonly router = inject(Router);
    readonly kinds = Object.keys(cacheDefinitionStores) as TypeKind[];
    readonly kindLabels = cacheTypeLabels;
    readonly kindNames = cacheTypeNames;
    readonly kindIcons: Record<TypeKind, string> = {
        items: "deployed_code", npcs: "group", objects: "deployed_code", animations: "animation", maps: "map", sprites: "image",
        quests: "task_alt", graphics: "auto_awesome", "varp-bits": "view_kanban", "client-map-definitions": "map",
        "config-definitions": "settings", cs2: "code", "cs2-int": "numbers",
    };
    readonly supportsNameSearch = computed(() => this.activeKind() === "items" || this.activeKind() === "npcs");
    readonly searchLabel = computed(() => (this.supportsNameSearch() ? "Name or ID" : "Definition ID"));
    readonly searchHelp = computed(() =>
        this.supportsNameSearch()
            ? "Search by name, enter an exact ID, or leave blank to browse ID ranges."
            : this.paginated()
              ? "Enter an exact ID, or leave blank to browse ID ranges."
              : "Enter an exact definition ID. Range browsing is unavailable for this type."
    );
    private readonly route = inject(ActivatedRoute);
    private readonly params = toSignal(this.route.queryParamMap, { initialValue: this.route.snapshot.queryParamMap });
    readonly activeKind = computed<TypeKind>(() => this.kinds.find((kind) => kind === this.params().get("kind")) ?? "items");
    private readonly appliedQuery = computed(() => this.params().get("query")?.trim() ?? "");
    readonly pageSize = computed(() => this.validNumber(this.params().get("limit"), 10, 1, 200));
    private readonly offset = computed(() => {
        const offset = this.validNumber(this.params().get("offset"), 0, 0, Number.MAX_SAFE_INTEGER - 200);
        return Math.floor(offset / this.pageSize()) * this.pageSize();
    });
    private readonly stores = {
        items: inject(cacheDefinitionStores.items),
        npcs: inject(cacheDefinitionStores.npcs),
        objects: inject(cacheDefinitionStores.objects),
        animations: inject(cacheDefinitionStores.animations),
        maps: inject(cacheDefinitionStores.maps),
        sprites: inject(cacheDefinitionStores.sprites),
        quests: inject(cacheDefinitionStores.quests),
        graphics: inject(cacheDefinitionStores.graphics),
        "varp-bits": inject(cacheDefinitionStores["varp-bits"]),
        "client-map-definitions": inject(cacheDefinitionStores["client-map-definitions"]),
        "config-definitions": inject(cacheDefinitionStores["config-definitions"]),
        cs2: inject(cacheDefinitionStores.cs2),
        "cs2-int": inject(cacheDefinitionStores["cs2-int"]),
    };
    get store() {
        return this.stores[this.activeKind()];
    }
    readonly resultKind = signal<TypeKind | null>(null);
    readonly resultColumns = computed(() => (this.activeKind() === "sprites" || this.activeKind() === "items" ? ["id", "preview", "name", "actions"] : ["id", "name", "actions"]));
    readonly browseError = signal<string | null>(null);
    readonly loading = computed(() => {
        const store = this.store;
        return (
            this.archives.loading() ||
            (this.appliedQuery() ? "searchLoading" in store && store.searchLoading() : "rangeLoading" in store && store.rangeLoading())
        );
    });
    readonly loadingFeedbackVisible = signal(false);
    readonly error = computed(() => {
        const store = this.store;
        return (
            this.archives.error() ??
            (this.appliedQuery() ? ("searchError" in store ? store.searchError() : null) : "rangeError" in store ? store.rangeError() : null)
        );
    });
    readonly pageIndex = computed(() => Math.floor(this.offset() / this.pageSize()));
    readonly paginated = computed(() => this.activeKind() !== "maps" && this.activeKind() !== "cs2-int");
    readonly paginationLength = computed(() => {
        if (this.resultKind() !== this.activeKind()) return 0;
        const store = this.store;
        if (this.appliedQuery()) return "searchResult" in store ? (store.searchResult()?.totalMatches ?? 0) : 0;
        return this.archiveSize();
    });
    private readonly paginator = viewChild(MatPaginator);
    readonly visiblePagination = computed(() => {
        const store = this.store;
        if (this.appliedQuery()) return "searchPagination" in store ? store.searchPagination() : null;
        return "rangePagination" in store ? store.rangePagination() : null;
    });
    readonly paginatorPageIndex = computed(() => this.visiblePagination()?.pageIndex ?? this.pageIndex());
    readonly paginatorPageSize = computed(() => this.visiblePagination()?.pageSize ?? this.pageSize());
    readonly rows = computed(() => {
        if (this.resultKind() !== this.activeKind() || this.error() || this.browseError()) return [];
        const store = this.store;
        const source = this.appliedQuery() ? ("searchResult" in store ? store.searchResult()?.items : []) : "rangeResult" in store ? store.rangeResult() : [];
        return (source ?? []).filter(
            (row): row is { id: number; name?: string } => row !== null && typeof row === "object" && "id" in row && typeof row.id === "number"
        );
    });
    private readonly searchModel = linkedSignal(() => {
        this.activeKind();
        return { query: this.appliedQuery() };
    });
    readonly searchForm = form(this.searchModel);
    constructor() {
        this.archives.loadArchiveSizes();
        effect((onCleanup) => {
            if (!this.loading()) {
                this.loadingFeedbackVisible.set(false);
                return;
            }
            const timeout = setTimeout(() => this.loadingFeedbackVisible.set(true), 200);
            onCleanup(() => clearTimeout(timeout));
        });
        effect(() => {
            this.activeKind();
            this.appliedQuery();
            this.offset();
            this.pageSize();
            if (this.archives.archiveSizes()) untracked(() => this.loadPage());
        });
    }

    async selectKind(index: number): Promise<void> {
        const kind = this.kinds[index];
        if (this.loading() || !kind || kind === this.activeKind()) return;
        await this.navigate({ kind, query: null, offset: 0 });
    }
    async browse(): Promise<void> {
        if (this.loading()) return;
        const query = this.searchModel().query.trim();
        if (/^-?\d+$/.test(query)) this.openDefinition(query);
        else if (query === this.appliedQuery() && this.offset() === 0) this.loadPage();
        else await this.navigate({ query: query || null, offset: 0 });
    }
    async changePage(event: PageEvent): Promise<void> {
        const paginator = this.paginator();
        if (paginator) {
            paginator.pageIndex = this.paginatorPageIndex();
            paginator.pageSize = this.paginatorPageSize();
        }
        if (this.loading()) return;
        await this.navigate({ offset: event.pageIndex * event.pageSize, limit: event.pageSize });
    }
    retry(): void {
        if (this.loading()) return;
        if (this.archives.error()) this.archives.loadArchiveSizes();
        else this.loadPage();
    }
    private navigate(changes: Record<string, string | number | null>) {
        return this.router.navigate([], { relativeTo: this.route, queryParams: { ...this.context(), ...changes } });
    }
    private context(): Record<string, string | number | null> {
        return { kind: this.activeKind(), query: this.appliedQuery() || null, offset: this.offset(), limit: this.pageSize() };
    }
    private validNumber(value: string | null, fallback: number, min: number, max: number): number {
        const parsed = value !== null && /^\d+$/.test(value) ? Number(value) : NaN;
        return Number.isSafeInteger(parsed) && parsed >= min && parsed <= max ? parsed : fallback;
    }
    spritePreviewFrame(frameCount: number): number | null {
        if (frameCount === 0) return null;
        return frameCount === 1 ? 0 : 1;
    }
    private loadPage(): void {
        this.browseError.set(null);
        const query = this.appliedQuery();
        const store = this.store;
        const offset = this.offset(),
            limit = this.pageSize();
        if (/^-?\d+$/.test(query)) this.openDefinition(query, { replaceUrl: true });
        else if (query) {
            if ("search" in store && this.supportsNameSearch()) {
                this.resultKind.set(this.activeKind());
                store.search(query, offset, limit);
            } else this.browseError.set("Name search is available for items and NPCs. Enter an exact ID or clear the query to browse this type.");
        } else if ("loadRange" in store) {
            const size = this.archiveSize();
            if (!size) {
                this.browseError.set("This archive has no definitions to browse.");
                return;
            }
            if (offset >= size) {
                void this.router.navigate([], {
                    relativeTo: this.route,
                    queryParams: { ...this.context(), offset: Math.floor((size - 1) / limit) * limit },
                    replaceUrl: true,
                });
                return;
            }
            this.resultKind.set(this.activeKind());
            store.loadRange(offset, Math.min(offset + limit, size), limit);
        } else this.browseError.set("This type supports lookup by ID. Enter a definition ID above.");
    }
    private openDefinition(query: string, options: { replaceUrl?: boolean } = {}): void {
        const id = Number(query);
        if (!Number.isSafeInteger(id) || id < 0) {
            this.browseError.set("Enter a valid definition ID of zero or greater.");
            return;
        }
        const context = this.context();
        // An ID lookup is navigation, not an applied list search to replay on return.
        if (this.appliedQuery() === query) context.query = null;
        void this.router.navigate(["/cache/types", this.activeKind(), id], { queryParams: context, ...options });
    }
    private archiveSize(): number {
        const sizes = this.archives.archiveSizes();
        const kind = this.activeKind();
        if (!sizes || kind === "cs2-int") return 0;
        const key =
            kind === "varp-bits"
                ? "varpBits"
                : kind === "client-map-definitions"
                  ? "clientMapDefinitions"
                  : kind === "config-definitions"
                    ? "configDefinitions"
                    : kind;
        return sizes[key];
    }
}
