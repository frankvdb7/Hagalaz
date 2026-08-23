import { NgModule } from "@angular/core";
import { type Routes, RouterModule } from "@angular/router";

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
