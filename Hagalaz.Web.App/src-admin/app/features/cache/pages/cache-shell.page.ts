import { toSignal } from "@angular/core/rxjs-interop";
import { filter } from "rxjs";
import { Component, computed, inject } from "@angular/core";
import { MatTabsModule } from "@angular/material/tabs";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { NavigationEnd, Router, RouterLink, RouterOutlet } from "@angular/router";
import { cacheTypeLabels, type TypeKind } from "../services/cache.models";

@Component({
    selector: "cache-shell-page",
    imports: [MatTabsModule, MatCardModule, MatIconModule, RouterLink, RouterOutlet],
    templateUrl: "./cache-shell.page.html",
    host: { class: "block min-w-0" },
})
export class CacheShellPageComponent {
    private readonly router = inject(Router);
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
    };
    readonly kinds = Object.keys(this.kindIcons) as (TypeKind | "models")[];
    readonly kindLabels = { ...cacheTypeLabels, models: "Models" };
    readonly activeKind = computed(() => {
        const url = this.router.parseUrl(this.navigation()?.urlAfterRedirects ?? this.router.url);
        const segments = url.root.children["primary"]?.segments;
        if (segments?.[1]?.path === "models") return "models";
        const kind = segments?.[2]?.path ?? url.queryParams["kind"];
        return this.kinds.find((value) => value === kind) ?? "items";
    });
}
