import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { LoginFormComponent, LoginFormModel } from "@app/main/login/login-form/login-form.component";
import { MatCard, MatCardContent } from "@angular/material/card";
import { RouterLink } from "@angular/router";
import { ErrorPipe } from "@app/common/pipes/error/error.pipe";

import { LoadingComponent } from "@app/common/components/loading/loading.component";
import { CardTitleComponent } from "@app/common/components/card-title/card-title.component";
import { AuthStore } from "@app/core/auth/auth.store";

@Component({
    selector: "app-login",
    templateUrl: "./login.component.html",
    styleUrls: ["./login.component.scss"],
    imports: [MatCard, MatCardContent, RouterLink, ErrorPipe, LoginFormComponent, LoadingComponent, CardTitleComponent],
    host: { class: "flex flex-auto" },
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent {
    readonly store = inject(AuthStore);

    onLoginSubmit(model: LoginFormModel) {
        this.store.login({ email: model.email, password: model.password, captcha: model.captcha });
    }
}
