import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";

const routes: Routes = [
    {
        path: "",
        loadComponent: () => import("./news.component").then((c) => c.NewsComponent),
    },
    { path: "all", loadComponent: () => import("./news-list/news-list.component").then((c) => c.NewsListComponent) },
    {
        path: ":id",
        loadComponent: () => import("./news-detail/news-detail.component").then((c) => c.NewsDetailComponent),
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
})
export class NewsRoutingModule {}
