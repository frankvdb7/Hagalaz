import { Component, ChangeDetectionStrategy, ChangeDetectorRef, inject, input, output, signal } from "@angular/core";
import { email, form, FormField, required } from "@angular/forms/signals";
import { MatButton } from "@angular/material/button";
import { MatError, MatFormField, MatLabel, MatSuffix } from "@angular/material/form-field";
import { MatIcon } from "@angular/material/icon";
import { MatInput } from "@angular/material/input";
import { NgHcaptchaModule } from "ng-hcaptcha";

export interface LoginFormModel {
    email: string;
    password: string;
    captcha: string;
}

@Component({
    selector: "app-login-form",
    templateUrl: "./login-form.component.html",
    styleUrls: ["./login-form.component.scss"],
    imports: [MatFormField, MatIcon, MatLabel, MatError, NgHcaptchaModule, FormField, MatInput, MatButton, MatSuffix],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginFormComponent {
    private change = inject(ChangeDetectorRef);

    get emailControl() {
        return this.form.email;
    }

    get passwordControl() {
        return this.form.password;
    }

    get captchaControl() {
        return this.form.captcha;
    }

    readonly error = input<unknown>();
    readonly loginSubmit = output<LoginFormModel>();

    submitted = false;

    private readonly model = signal<LoginFormModel>({ email: "", password: "", captcha: "" });

    readonly form = form(this.model, (path) => {
        required(path.email);
        email(path.email);
        required(path.password);
        required(path.captcha);
    });

    markForCheck() {
        this.change.markForCheck();
    }

    onSubmit(event: SubmitEvent) {
        event.preventDefault();
        this.submitted = true;
        if (this.form().invalid()) {
            return;
        }
        this.loginSubmit.emit(this.model());
    }
}
