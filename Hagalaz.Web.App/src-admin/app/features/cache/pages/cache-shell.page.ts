import { toSignal } from "@angular/core/rxjs-interop";
import { filter } from "rxjs";
import { afterRenderEffect, Component, computed, inject, viewChild } from "@angular/core";
import { MatTabNav, MatTabsModule } from "@angular/material/tabs";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { NavigationEnd, Router, RouterLink, RouterOutlet, type UrlTree } from "@angular/router";
import { cacheTypeLabels, type TypeKind } from "../services/cache.models";

type CacheTab = TypeKind | "models" | "widgets";

/** `/cache/models` and `/cache/widgets` are their own tabs; other cache pages name their type in the path or `kind` query. */
function cacheTabFromUrl(url: UrlTree): string | undefined {
    const paths = (url.root.children.primary?.segments ?? []).map(segment => segment.path);
    const section = paths[1];
    if (section === "models" || section === "widgets") return section;
    return paths[2] ?? url.queryParams.kind;
}

@Component({
    selector: "cache-shell-page",
    imports: [MatTabsModule, MatCardModule, MatIconModule, RouterLink, RouterOutlet],
    templateUrl: "./cache-shell.page.html",
    styles: ":host { display: block; } .widget-tabs a { min-width: 0; padding-inline: 12px; }",
    host: { class: "min-w-0" },
})
export class CacheShellPageComponent {
    private readonly router = inject(Router);
    private readonly tabs = viewChild(MatTabNav);
    private readonly navigation = toSignal(this.router.events.pipe(filter((event) => event instanceof NavigationEnd)));
    readonly kindIcons = {
        items: "deployed_code",
        npcs: "group",
        objects: "deployed_code",
        animations: "animation",
        maps: "map",
        sprites: "image",
        quests: "task_alt",
        graphics: "auto_awesome",
        "varp-bits": "view_kanban",
        "client-map-definitions": "map",
        "config-definitions": "settings",
        cs2: "code",
        "cs2-int": "numbers",
        models: "view_in_ar",
        widgets: "widgets",
    };
    readonly kinds = Object.keys(this.kindIcons) as CacheTab[];
    readonly kindLabels = { ...cacheTypeLabels, models: "Models", widgets: "Widgets" };
    readonly activeKind = computed(() => {
        const url = this.router.parseUrl(this.navigation()?.urlAfterRedirects ?? this.router.url);
        const kind = cacheTabFromUrl(url);
        return this.kinds.find((value) => value === kind) ?? "items";
    });

    constructor() {
        afterRenderEffect(() => {
            const tabs = this.tabs();
            if (this.activeKind() === "widgets" && tabs) {
                // Widgets is the last tab, and Material clamps this to its scroll range.
                tabs.scrollDistance = Number.MAX_SAFE_INTEGER;
            }
        });
    }

    tabPath(kind: CacheTab): string {
        return kind === "models" || kind === "widgets" ? `/cache/${kind}` : "/cache/types";
    }
}
