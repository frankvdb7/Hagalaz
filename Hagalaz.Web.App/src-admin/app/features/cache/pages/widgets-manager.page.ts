import { Component, computed, inject } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatPaginatorModule, type PageEvent } from "@angular/material/paginator";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatTableModule } from "@angular/material/table";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { CacheWidgetsService } from "../services/cache-widgets.service";

@Component({
    selector: "cache-widgets-manager-page",
    imports: [MatButtonModule, MatCardModule, MatPaginatorModule, MatProgressBarModule, MatTableModule, RouterLink],
    templateUrl: "./widgets-manager.page.html",
})
export class WidgetsManagerPageComponent {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly params = toSignal(this.route.queryParamMap, { initialValue: this.route.snapshot.queryParamMap });
    readonly cache = inject(CacheWidgetsService);
    readonly columns = ["id", "components"];
    readonly pageSizeOptions = [10, 25, 50, 100, 200];
    private readonly defaultPageSize = 10;
    readonly pageSize = computed(() => {
        const limit = Number(this.params().get("limit"));
        return this.pageSizeOptions.includes(limit) ? limit : this.defaultPageSize;
    });
    readonly pageIndex = computed(() => {
        const page = Number(this.params().get("page"));
        const lastPage = Math.max(0, Math.ceil(this.cache.widgets().length / this.pageSize()) - 1);
        return Number.isSafeInteger(page) && page > 0 ? Math.min(page, lastPage) : 0;
    });
    readonly rows = computed(() => {
        const start = this.pageIndex() * this.pageSize();
        return this.cache.widgets().slice(start, start + this.pageSize());
    });

    changePage(event: PageEvent): void {
        void this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { page: event.pageIndex || null, limit: event.pageSize === this.defaultPageSize ? null : event.pageSize },
        });
    }
}
