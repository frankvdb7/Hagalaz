import { Routes } from "@angular/router";
import { mainRoutes } from "@app/main/main-routing";

const launcherRoutes: Routes = [
    {
        path: "",
        outlet: "header",
        loadChildren: () => import("@app/launcher/header/header-routing").then((m) => m.routes),
    },
    {
        path: "",
        outlet: "footer",
        loadChildren: () => import("@app/launcher/footer/footer-routing").then((m) => m.routes),
    },
    ...mainRoutes,
];

export const routes: Routes = [
    {
        path: "",
        loadComponent: () => import("./launcher.component").then((c) => c.LauncherComponent),
        children: launcherRoutes,
    },
];
