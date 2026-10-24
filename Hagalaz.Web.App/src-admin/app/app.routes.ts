import type { Routes } from "@angular/router";
import { adminCanActivate } from "./core/guards/admin.guard";
import { CachePendingChangesService, cacheCanActivateChild, cacheCanDeactivate } from "./features/cache/cache-pending-changes.service";
import { cacheDetailRoutes } from "./features/cache/cache-detail.routes";
import { integrationRoutes } from "./pages/integration.routes";

export const routes: Routes = [
    { path: "login", loadComponent: () => import("./pages/login.page").then((m) => m.LoginPageComponent) },
    { path: "denied", loadComponent: () => import("./pages/denied.page").then((m) => m.AccessDeniedPageComponent) },
    {
        path: "",
        canActivate: [adminCanActivate],
        canActivateChild: [adminCanActivate],
        loadComponent: () => import("./layout/portal-shell.page").then((m) => m.PortalShellPageComponent),
        children: [
            { path: "", pathMatch: "full", redirectTo: "overview" },
            { path: "overview", loadComponent: () => import("./pages/overview.page").then((m) => m.OverviewPageComponent) },
            {
                path: "cache",
                providers: [CachePendingChangesService],
                canActivateChild: [cacheCanActivateChild],
                canDeactivate: [cacheCanDeactivate],
                loadComponent: () => import("./features/cache/pages/cache-shell.page").then((m) => m.CacheShellPageComponent),
                children: [
                    ...cacheDetailRoutes,
                    { path: "types", loadComponent: () => import("./features/cache/pages/types-manager.page").then((m) => m.TypesManagerPageComponent) },
                    { path: "sprites", loadComponent: () => import("./features/cache/pages/sprites-manager.page").then((m) => m.SpritesManagerPageComponent) },
                    { path: "", pathMatch: "full", redirectTo: "types" },
                ],
            },
            ...integrationRoutes,
        ],
    },
    { path: "**", redirectTo: "overview" },
];
