import { Routes } from "@angular/router";
import { adminCanActivate } from "./core/guards/admin.guard";

export const routes: Routes = [
    {
        path: "",
        pathMatch: "full",
        redirectTo: "portal/dashboard",
    },
    {
        path: "login",
        loadComponent: () => import("./pages/login.page").then((m) => m.LoginPageComponent),
    },
    {
        path: "denied",
        loadComponent: () => import("./pages/denied.page").then((m) => m.AccessDeniedPageComponent),
    },
    {
        path: "portal",
        canActivate: [adminCanActivate],
        loadComponent: () => import("./features/cache/pages/cache-shell.page").then((m) => m.CacheShellPageComponent),
        data: { breadcrumb: "Portal" },
        children: [
            {
                path: "dashboard",
                loadComponent: () => import("./features/cache/pages/dashboard/dashboard.page").then((m) => m.DashboardPageComponent),
                data: { breadcrumb: "Dashboard" },
            },
            {
                path: "cache",
                data: { breadcrumb: "Cache & Assets" },
                children: [
                    {
                        path: "types",
                        loadComponent: () => import("./features/cache/pages/types-manager.page").then((m) => m.TypesManagerPageComponent),
                        data: { breadcrumb: "Definitions" },
                    },
                    {
                        path: "sprites",
                        loadComponent: () => import("./features/cache/pages/sprites-manager.page").then((m) => m.SpritesManagerPageComponent),
                        data: { breadcrumb: "Sprites" },
                    },
                    {
                        path: "explorer",
                        loadComponent: () => import("./features/cache/pages/cache-explorer/cache-explorer.page").then((m) => m.CacheExplorerPageComponent),
                        data: { breadcrumb: "Explorer" },
                    },
                    {
                        path: "maps",
                        loadComponent: () => import("./features/cache/pages/maps-manager.page").then((m) => m.MapsManagerPageComponent),
                        data: { breadcrumb: "Maps" },
                    },
                ],
            },
            {
                path: "characters",
                data: { breadcrumb: "Characters" },
                children: [
                    {
                        path: "search",
                        loadComponent: () => import("./features/characters/pages/character-search/character-search.page").then((m) => m.CharacterSearchPageComponent),
                        data: { breadcrumb: "Account Lookup" },
                    },
                    {
                        path: "offences",
                        loadComponent: () => import("./pages/denied.page").then((m) => m.AccessDeniedPageComponent),
                        data: { breadcrumb: "Moderation" },
                    },
                ],
            },
            {
                path: "world",
                data: { breadcrumb: "Infrastructure" },
                children: [
                    {
                        path: "status",
                        loadComponent: () => import("./features/world/pages/world-status/world-status.page").then((m) => m.WorldStatusPageComponent),
                        data: { breadcrumb: "Server Status" },
                    },
                    {
                        path: "spawns",
                        loadComponent: () => import("./pages/denied.page").then((m) => m.AccessDeniedPageComponent),
                        data: { breadcrumb: "Entity Spawns" },
                    },
                ],
            },
            {
                path: "logs",
                data: { breadcrumb: "Logs" },
                children: [
                    {
                        path: "chat",
                        loadComponent: () => import("./pages/denied.page").then((m) => m.AccessDeniedPageComponent),
                        data: { breadcrumb: "Chat Logs" },
                    },
                    {
                        path: "audit",
                        loadComponent: () => import("./pages/denied.page").then((m) => m.AccessDeniedPageComponent),
                        data: { breadcrumb: "Audit Logs" },
                    },
                ],
            },
            {
                path: "",
                pathMatch: "full",
                redirectTo: "dashboard",
            },
        ],
    },
    {
        path: "**",
        redirectTo: "",
    },
];
