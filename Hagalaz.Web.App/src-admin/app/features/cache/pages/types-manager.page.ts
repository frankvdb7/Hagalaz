import { Component, computed, effect, inject, signal, untracked } from "@angular/core";
import { FormField, form, max, min, required } from "@angular/forms/signals";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatPaginatorModule, type PageEvent } from "@angular/material/paginator";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatTableModule } from "@angular/material/table";
import { MatTabsModule } from "@angular/material/tabs";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { cacheTypeLabels, type TypeKind } from "../services/cache.models";
import { CacheArchivesStore } from "../stores/cache-archives.store";
import { cacheDefinitionStores } from "../stores/cache-definition-stores";

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
        MatPaginatorModule,
        MatProgressBarModule,
    ],
    providers: [CacheArchivesStore, ...Object.values(cacheDefinitionStores)],
    templateUrl: "./types-manager.page.html",
    styleUrl: "./types-manager.page.scss",
})
export class TypesManagerPageComponent {
    readonly archives = inject(CacheArchivesStore);
    private readonly router = inject(Router);
    readonly kinds = Object.keys(cacheDefinitionStores) as TypeKind[];
    readonly kindLabels = cacheTypeLabels;
    private readonly initialKind = inject(ActivatedRoute).snapshot.queryParamMap.get("kind");
    readonly activeKind = signal<TypeKind>(this.kinds.find((kind) => kind === this.initialKind) ?? "items");
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
    readonly resultColumns = ["id", "name"];
    readonly browseError = signal<string | null>(null);
    private readonly appliedQuery = signal("");
    readonly loading = computed(() => {
        const store = this.store;
        return this.archives.loading() || ("rangeLoading" in store && store.rangeLoading()) || ("searchLoading" in store && store.searchLoading());
    });
    readonly error = computed(() => {
        const store = this.store;
        return (
            this.archives.error() ??
            (this.appliedQuery() ? ("searchError" in store ? store.searchError() : null) : "rangeError" in store ? store.rangeError() : null)
        );
    });
    readonly pageIndex = computed(() => Math.floor(this.searchForm.offset().value() / this.searchForm.limit().value()));
    readonly paginated = computed(() => this.activeKind() !== "maps" && this.activeKind() !== "cs2-int");
    readonly paginationLength = computed(() => {
        if (this.resultKind() !== this.activeKind()) return 0;
        const store = this.store;
        if (this.appliedQuery()) return "searchResult" in store ? (store.searchResult()?.totalMatches ?? 0) : 0;
        return this.archiveSize();
    });
    readonly rows = computed(() => {
        if (this.resultKind() !== this.activeKind() || this.error() || this.browseError()) return [];
        const store = this.store;
        const source = this.appliedQuery() ? ("searchResult" in store ? store.searchResult()?.items : []) : "rangeResult" in store ? store.rangeResult() : [];
        return (source ?? []).filter(
            (row): row is { id: number; name?: string } => row !== null && typeof row === "object" && "id" in row && typeof row.id === "number"
        );
    });
    private readonly searchModel = signal({ query: "", offset: 0, limit: 10 });
    readonly searchForm = form(this.searchModel, (path) => {
        required(path.offset);
        min(path.offset, 0);
        required(path.limit);
        min(path.limit, 1);
        max(path.limit, 200);
    });
    constructor() {
        this.archives.loadArchiveSizes();
        effect(() => {
            if (this.archives.archiveSizes()) untracked(() => this.loadPage());
        });
    }

    selectKind(index: number): void {
        if (this.loading()) return;
        const kind = this.kinds[index];
        if (kind === this.activeKind()) return;
        this.activeKind.set(kind);
        this.resultKind.set(null);
        this.searchModel.update((value) => ({ ...value, query: "", offset: 0 }));
        this.appliedQuery.set("");
        this.loadPage();
    }
    browse(): void {
        if (this.loading() || this.searchForm().invalid()) return;
        this.searchModel.update((value) => ({ ...value, offset: 0 }));
        this.appliedQuery.set(this.searchModel().query.trim());
        this.loadPage();
    }
    changePage(event: PageEvent): void {
        if (this.loading()) return;
        this.searchModel.update((value) => ({ ...value, offset: event.pageIndex * event.pageSize, limit: event.pageSize }));
        this.loadPage();
    }
    private loadPage(): void {
        if (this.searchForm().invalid()) return;
        this.browseError.set(null);
        const query = this.appliedQuery();
        const store = this.store;
        const { offset, limit } = this.searchModel();
        if (/^-?\d+$/.test(query)) {
            const id = Number(query);
            if (!Number.isSafeInteger(id) || id < 0) {
                this.browseError.set("Enter a valid definition ID of zero or greater.");
                return;
            }
            void this.router.navigate(["/cache/types", this.activeKind(), id]);
        } else if (query) {
            if ("search" in store) {
                this.resultKind.set(this.activeKind());
                store.search(query, offset, limit);
            } else this.browseError.set("Name search is available for items, NPCs and sprites. Enter an ID or clear the query to browse this type.");
        } else if ("loadRange" in store) {
            const size = this.archiveSize();
            if (!size) {
                this.browseError.set("This archive has no definitions to browse.");
                return;
            }
            this.resultKind.set(this.activeKind());
            store.loadRange(offset, Math.min(offset + limit, size));
        } else this.browseError.set("This type supports lookup by ID. Enter a definition ID above.");
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
