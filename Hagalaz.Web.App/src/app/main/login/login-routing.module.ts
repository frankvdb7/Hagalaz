import { NgModule } from "@angular/core";
import { type Routes, RouterModule } from "@angular/router";

const routes: Routes = [
    {
        path: "",
        loadComponent: () => import("./login.component").then((c) => c.LoginComponent),
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
})
export class LoginRoutingModule {}
