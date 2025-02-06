import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";

const routes: Routes = [
    {
        path: "",
        loadComponent: () => import("./highscores.component").then((c) => c.HighscoresComponent),
        data: {
            title: "Highscores",
        },
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
})
export class HighscoresRoutingModule {}
