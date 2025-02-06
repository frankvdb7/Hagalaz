import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";

const routes: Routes = [
    {
        path: "",
        loadComponent: () => import("./play.component").then((c) => c.PlayComponent),
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
})
export class PlayRoutingModule {}
