import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";
import { RegisterComponent } from "./register.component";

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
