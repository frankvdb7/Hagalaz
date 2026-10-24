import { Component, computed, inject } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { CacheArchivesStore } from "../features/cache/stores/cache-archives.store";

@Component({
    selector: "admin-archive-count-card",
    imports: [MatButtonModule, MatCardModule, MatIconModule],
    providers: [CacheArchivesStore],
    template: `<mat-card appearance="outlined"><mat-card-content>
        <div class="flex items-center justify-between gap-3"><h2>Cache definitions</h2><mat-icon>database</mat-icon></div>
        <p class="count mt-6">{{ definitionCount() ?? "Not loaded" }}</p>
        <button mat-button type="button" [disabled]="cache.loading()" (click)="cache.loadArchiveSizes()">{{ loadLabel() }}</button>
        @if (cache.error()) { <p role="alert" class="accent-ember mt-2">{{ cache.error() }}</p> }
    </mat-card-content></mat-card>`,
    styles: `:host { display: block; height: 100%; } mat-card { height: 100%; position: relative; overflow: hidden; } mat-card::before { content: ""; position: absolute; top: 0; left: 20px; height: 3px; width: 40%; background: var(--mat-sys-primary); } mat-icon { color: var(--mat-sys-primary); } h2 { font: var(--mat-sys-body-small); color: var(--mat-sys-on-surface-variant); } .count { font: var(--mat-sys-title-large); font-weight: 700; }`,
})
export class ArchiveCountCardComponent {
    readonly cache = inject(CacheArchivesStore);
    readonly definitionCount = computed(() => {
        const sizes = this.cache.archiveSizes();
        return sizes
            ? Object.values(sizes)
                  .reduce((total, count) => total + count, 0)
                  .toLocaleString()
            : null;
    });
    readonly loadLabel = computed(() => {
        if (this.cache.loading()) return "Loading…";
        return this.definitionCount() === null ? "Load archive counts" : "Refresh counts";
    });
}
