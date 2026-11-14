import {Component, computed, inject, viewChild} from "@angular/core";
import {toSignal} from "@angular/core/rxjs-interop";
import {ActivatedRoute, Router, RouterLink} from "@angular/router";
import {MatIconModule} from "@angular/material/icon";
import {PortalBreadcrumbDirective} from "../../../../layout/portal-breadcrumb.directive";
import {injectCacheDefinitionId} from "../cache-definition-id";
import {CacheModelPreviewComponent} from "../cache-model-preview.component";
import {ModelLookupComponent} from "../model-lookup.component";

const inspectionTabs = ["model", "textures", "recolors", "retextures", "animations", "metadata"];

@Component({
    selector: "cache-models-detail-page",
    imports: [RouterLink, MatIconModule, PortalBreadcrumbDirective, CacheModelPreviewComponent, ModelLookupComponent],
    template: `<ng-template portalBreadcrumb><a class="breadcrumb supporting-text" routerLink="/cache/models" [queryParams]="browseContext()">Models</a><mat-icon class="breadcrumb breadcrumb-separator supporting-text" aria-hidden="true">chevron_right</mat-icon><span class="breadcrumb">Model #{{ id() ?? 'Invalid ID' }}</span></ng-template>
        @if (id() !== null) {
            <cache-model-preview [modelId]="id()" [workspace]="true" [inspectionTab]="tab()" (inspectionTabChange)="selectTab($event)">
                <cache-model-lookup modelLookup [id]="id()" (selectModel)="load($event)" />
            </cache-model-preview>
        } @else {
            <p role="alert" class="mb-3">Enter a valid model ID in the URL.</p>
            <cache-model-lookup (selectModel)="load($event)" />
        }`,
    host: {class: "block min-w-0"},
})
export class ModelsDetailPageComponent {
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly query = toSignal(this.route.queryParamMap, {initialValue: this.route.snapshot.queryParamMap});
    private readonly preview = viewChild(CacheModelPreviewComponent);
    readonly id = injectCacheDefinitionId();
    readonly browseContext = computed(() => ({offset: this.query().get("offset"), limit: this.query().get("limit")}));
    readonly tab = computed(() => Math.max(0, inspectionTabs.indexOf(this.query().get("tab") ?? "model")));

    load(id: number): void {
        if (id === this.id()) this.preview()?.model.reload();
        else void this.router.navigate(["/cache/models", id], {queryParams:this.browseContext()});
    }

    selectTab(index: number): void {
        if (index === this.tab() || !inspectionTabs[index]) return;
        void this.router.navigate([], {relativeTo: this.route, queryParamsHandling: "merge", queryParams: {tab: index === 0 ? null : inspectionTabs[index]}});
    }
}
