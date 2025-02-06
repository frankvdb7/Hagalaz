import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";
import { authCanActivate } from "@app/core/auth/auth.guard";

const routes: Routes = [
    {
        path: "",
        loadComponent: () => import("./profile.component").then((c) => c.ProfileComponent),
        canActivate: [authCanActivate],
        children: [
            {
                path: "",
                redirectTo: "overview",
                pathMatch: "full",
            },
            {
                path: "overview",
                loadComponent: () => import("./overview/overview.component").then((c) => c.OverviewComponent),
            },
            {
                path: "pvp",
                loadComponent: () => import("./pvp/pvp.component").then((c) => c.PvpComponent),
            },
        ],
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ProfileRoutingModule {}
