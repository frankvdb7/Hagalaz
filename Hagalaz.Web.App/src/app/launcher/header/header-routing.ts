import { Routes } from "@angular/router";

export const routes: Routes = [
    {
        path: "",
        loadComponent: () => import("./header.component").then((c) => c.LauncherHeaderComponent),
    },
];
