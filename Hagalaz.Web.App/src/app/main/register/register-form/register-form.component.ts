import { Component, ChangeDetectionStrategy, signal } from "@angular/core";
import { email, form, FormField, required } from "@angular/forms/signals";
import { MatButton } from "@angular/material/button";
import { MatError, MatFormField, MatLabel, MatSuffix } from "@angular/material/form-field";
import { MatInput } from "@angular/material/input";
import { RouterLink } from "@angular/router";
import { MatCheckbox } from "@angular/material/checkbox";
import { MatIcon } from "@angular/material/icon";

@Component({
    selector: "app-register-form",
    templateUrl: "./register-form.component.html",
    styleUrls: ["./register-form.component.scss"],
    imports: [MatButton, MatInput, MatFormField, MatLabel, MatError, RouterLink, MatCheckbox, FormField, MatIcon, MatSuffix],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterFormComponent {
    private readonly model = signal({ email: "", password: "", passwordConfirm: "", accepted: false });

    readonly form = form(this.model, (path) => {
        required(path.email);
        email(path.email);
        required(path.password);
        required(path.passwordConfirm);
        required(path.accepted);
    });

    onSubmit(event: SubmitEvent) {
        event.preventDefault();
        if (this.form().invalid()) {
            return;
        }
    }
}
