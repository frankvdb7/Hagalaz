import { ChangeDetectionStrategy, Component } from "@angular/core";
import { RegisterFormComponent } from "@app/main/register/register-form/register-form.component";
import { CardTitleComponent } from "@app/common/components/card-title/card-title.component";
import { LoadingComponent } from "@app/common/components/loading/loading.component";
import { RouterLink } from "@angular/router";
import { MatCard, MatCardContent } from "@angular/material/card";

@Component({
    selector: "app-register",
    templateUrl: "./register.component.html",
    styleUrls: ["./register.component.scss"],
    imports: [CardTitleComponent, RouterLink, LoadingComponent, RegisterFormComponent, MatCard, MatCardContent],
    host: { class: "flex flex-auto" },
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent {
}
