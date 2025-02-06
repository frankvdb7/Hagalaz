import { Routes } from "@angular/router";
import { environment } from "@environment/environment";

export function getAppRoutes(): Routes {
    const routes: Routes = [
        {
            path: "**",
            redirectTo: "",
        },
    ];

    if (environment.name === "launcher") {
        routes.unshift({
            path: "",
            loadChildren: () => import("@app/launcher/launcher-routing").then((m) => m.routes),
        });
    } else {
        routes.unshift({
            path: "",
            loadChildren: () => import("@app/main/main-routing").then((m) => m.routes),
        });
    }
    return routes;
}
