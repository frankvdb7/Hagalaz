import { JsonPipe } from "@angular/common";
import { Component, computed, effect, inject, untracked } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { RouterLink } from "@angular/router";
import { PortalBreadcrumbDirective } from "../../../../layout/portal-breadcrumb.directive";
import { CacheCs2Store } from "../../stores/cache-cs2.store";
import { injectCacheDefinitionId } from "../cache-definition-id";
import { DefinitionPickerComponent } from "../definition-picker.component";

@Component({
    selector: "cache-cs2-detail-page",
    imports: [DefinitionPickerComponent, JsonPipe, MatButtonModule, MatCardModule, MatIconModule, MatProgressBarModule, PortalBreadcrumbDirective, RouterLink],
    providers: [CacheCs2Store],
    templateUrl: "./cs2-detail.page.html",
    styleUrl: "./cs2-detail.page.scss",
    host: { class: "block min-w-0" },
})
export class Cs2DetailPageComponent {
    readonly store = inject(CacheCs2Store);
    readonly id = injectCacheDefinitionId();
    readonly script = computed(() => {
        const script = this.store.byIdResult();
        return script?.id === this.id() ? script : null;
    });
    readonly source = computed(() => {
        const lines = this.script()?.source ?? [];
        return { lines, partial: lines.some((line) => line.raw) };
    });

    constructor() {
        effect(() => {
            const id = this.id();
            if (id !== null) untracked(() => this.store.loadById(id));
        });
    }

    retry(): void {
        const id = this.id();
        if (id !== null) this.store.loadById(id);
    }
}
