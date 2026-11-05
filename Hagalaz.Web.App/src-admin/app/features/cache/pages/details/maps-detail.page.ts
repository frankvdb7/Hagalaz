import { Component, computed, effect, inject, linkedSignal, signal, untracked } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { FormField, form, min, max } from "@angular/forms/signals";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MatExpansionModule } from "@angular/material/expansion";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { PortalBreadcrumbDirective } from "../../../../layout/portal-breadcrumb.directive";
import { parseMapKeys, selectedMapIds } from "../../services/map-render.models";
import { CacheMapSceneStore } from "../../stores/cache-map-scene.store";
import { injectCacheDefinitionId } from "../cache-definition-id";
import { buildMapScene, MapScenePreparation } from "../map-scene-geometry";
import { MapViewportComponent } from "../map-viewport.component";

@Component({
    selector: "cache-maps-detail-page",
    imports: [FormField, RouterLink, PortalBreadcrumbDirective, MapViewportComponent, MatButtonModule, MatCardModule, MatCheckboxModule, MatExpansionModule, MatFormFieldModule, MatInputModule, MatProgressBarModule],
    providers: [CacheMapSceneStore],
    templateUrl: "./maps-detail.page.html",
    host: { class: "block min-w-0" },
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
    readonly returnParams = computed(() => ({ kind: "maps", query: this.params().get("query"), offset: this.params().get("offset"), limit: this.params().get("limit") }));
    private readonly draft = linkedSignal(() => ({ width: this.selection().width, height: this.selection().height }));
    readonly dimensions = form(this.draft, path => { min(path.width, 1); max(path.width, 3); min(path.height, 1); max(path.height, 3); });
    private readonly keyDraft = signal({ text: "" });
    readonly keyForm = form(this.keyDraft);
    private keys: Record<string, number[]> = {};
    readonly error = signal<string | null>(null);
    readonly planes = signal([true, false, false, false]);
    readonly landscape = signal(true);
    private readonly preparation = linkedSignal(() => { this.selectionKey(); return new MapScenePreparation(); });
    readonly scene = computed(() => buildMapScene(this.store.regions(), this.store.appearances(), this.store.textures(), this.planes(), this.landscape(), this.preparation()));
    readonly loadingText = computed(() => {
        const progress = this.store.progress();
        const stage = this.store.regions().length ? "Loading visible scene assets" : "Loading terrain";
        return `${stage}. Regions ${progress.regionsSettled}/${progress.regionsTotal}. Appearances ${progress.appearancesSettled}/${progress.appearancesTotal}. Textures ${progress.texturesSettled}/${progress.texturesTotal}. ${progress.failures} failed reads.`;
    });
    readonly warnings = computed(() => [...new Set([...this.store.warnings(), ...this.scene().warnings])]);

    constructor() {
        effect(() => {
            const value = { planes: this.planes(), landscape: this.landscape() };
            untracked(() => this.store.setVisibility(value));
        });
        effect(() => {
            const selection = this.selection();
            untracked(() => this.store.load({ ...selection, keys: this.keys }));
        });
    }
    async apply(): Promise<void> {
        const selection = { id: this.id() ?? NaN, ...this.draft() };
        const ids = selectedMapIds(selection);
        this.error.set(null);
        if (!ids.length) { this.error.set("Enter a valid region ID and dimensions from 1 to 3 without crossing region coordinate 255."); return; }
        try { this.keys = parseMapKeys(this.keyDraft().text, ids); }
        catch (error) { this.error.set(error instanceof Error ? error.message : "Invalid region keys."); return; }
        if (selection.width === this.selection().width && selection.height === this.selection().height) this.retry();
        else await this.router.navigate([], { relativeTo: this.route, queryParamsHandling: "merge", queryParams: { width: selection.width === 1 ? null : selection.width, height: selection.height === 1 ? null : selection.height } });
    }
    retry(): void { if (!this.store.loading()) this.store.load({ ...this.selection(), keys: this.keys }); }
    togglePlane(plane: number, checked: boolean): void { this.planes.update(planes => planes.map((value, index) => index === plane ? checked : value)); }
}

function dimension(value: string | null): number {
    return value === null ? 1 : /^\d+$/.test(value) ? Number(value) : NaN;
}
