import { Routes } from "@angular/router";
import { adminCanActivate } from "./core/guards/admin.guard";

export const routes: Routes = [
    {
        path: "",
        pathMatch: "full",
        redirectTo: "cache/types",
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
        path: "cache",
        canActivate: [adminCanActivate],
        loadComponent: () => import("./features/cache/pages/cache-shell.page").then((m) => m.CacheShellPageComponent),
        children: [
            {
                path: "types",
                loadComponent: () => import("./features/cache/pages/types-manager.page").then((m) => m.TypesManagerPageComponent),
            },
            {
                path: "sprites",
                loadComponent: () => import("./features/cache/pages/sprites-manager.page").then((m) => m.SpritesManagerPageComponent),
            },
            {
                path: "",
                pathMatch: "full",
                redirectTo: "types",
            },
        ],
    },
    {
        path: "**",
        redirectTo: "",
    },
];
