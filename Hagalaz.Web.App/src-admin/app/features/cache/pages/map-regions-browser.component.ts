import { Component, computed, effect, inject, linkedSignal, signal, untracked } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { FormField, form } from "@angular/forms/signals";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatPaginatorModule, type PageEvent } from "@angular/material/paginator";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatTableModule } from "@angular/material/table";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import type { MapRegionSummaryDto } from "../services/dtos";
import { CacheMapRegionsStore } from "../stores/cache-map-regions.store";

@Component({
    selector: "cache-map-regions-browser",
    imports: [FormField, RouterLink, MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatPaginatorModule, MatProgressBarModule, MatTableModule],
    providers: [CacheMapRegionsStore],
    templateUrl: "./map-regions-browser.component.html",
    host: { class: "block min-w-0" },
})
export class MapRegionsBrowserComponent {
    readonly store = inject(CacheMapRegionsStore);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly params = toSignal(this.route.queryParamMap, { initialValue: this.route.snapshot.queryParamMap });
    readonly pageSize = computed(() => this.pageNumber("limit", 10, 1, 200));
    readonly offset = computed(() => Math.floor(this.pageNumber("offset", 0, 0, 65536) / this.pageSize()) * this.pageSize());
    readonly pageIndex = computed(() => this.store.rangePagination()?.pageIndex ?? Math.floor(this.offset() / this.pageSize()));
    readonly displayedPageSize = computed(() => this.store.rangePagination()?.pageSize ?? this.pageSize());
    readonly rows = computed<MapRegionSummaryDto[]>(() => this.store.rangeResult() ?? []);
    readonly paginationLength = computed(() => this.store.regionPage()?.total ?? 0);
    readonly browseError = signal<string | null>(null);
    private readonly searchModel = linkedSignal(() => ({ query: this.params().get("query")?.trim() ?? "" }));
    readonly searchForm = form(this.searchModel);
    readonly columns = ["id", "regionX", "regionY", "terrain", "landscape", "actions"];

    constructor() {
        effect(() => {
            const offset = this.offset(), limit = this.pageSize();
            untracked(() => this.store.loadPage(offset, limit));
        });
    }

    async browse(): Promise<void> {
        if (this.store.rangeLoading()) return;
        const query = this.searchModel().query.trim();
        this.browseError.set(null);
        if (!query) { this.store.loadPage(this.offset(), this.pageSize()); return; }
        const id = /^\d+$/.test(query) ? Number(query) : NaN;
        if (!Number.isSafeInteger(id) || id < 0 || id > 65535) {
            this.browseError.set("Enter a region ID between 0 and 65535.");
            return;
        }
        await this.router.navigate(["/cache/types/maps", id], { queryParams: { ...this.route.snapshot.queryParams, kind: "maps", query: null } });
    }

    async changePage(event: PageEvent): Promise<void> {
        if (this.store.rangeLoading()) return;
        await this.router.navigate([], { relativeTo: this.route, queryParamsHandling: "merge", queryParams: { offset: event.pageIndex * event.pageSize, limit: event.pageSize } });
    }

    private pageNumber(key: string, fallback: number, min: number, max: number): number {
        const value = this.params().get(key);
        const parsed = value !== null && /^\d+$/.test(value) ? Number(value) : NaN;
        return Number.isSafeInteger(parsed) && parsed >= min && parsed <= max ? parsed : fallback;
    }
}
