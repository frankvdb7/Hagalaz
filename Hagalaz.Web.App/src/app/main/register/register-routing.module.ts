import { NgModule } from "@angular/core";
import { type Routes, RouterModule } from "@angular/router";

const routes: Routes = [
    {
        path: "",
        loadComponent: () => import("./register.component").then((c) => c.RegisterComponent),
    },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
})
export class RegisterRoutingModule {}
