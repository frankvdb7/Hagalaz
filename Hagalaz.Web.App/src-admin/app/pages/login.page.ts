import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, signal } from "@angular/core";
import { email, form, FormField, required } from "@angular/forms/signals";
import { Router } from "@angular/router";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { NgHcaptchaModule } from "ng-hcaptcha";
import { AuthStore } from "../core/auth/auth.store";

@Component({
    standalone: true,
    selector: "admin-login-page",
    imports: [FormField, MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressBarModule, NgHcaptchaModule],
    templateUrl: "./login.page.html",
    styleUrl: "./login.page.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPageComponent {
    private readonly change = inject(ChangeDetectorRef);
    readonly store = inject(AuthStore);
    private readonly router = inject(Router);

    private readonly model = signal({ email: "", password: "", captcha: "" });

    readonly form = form(this.model, (path) => {
        required(path.email);
        email(path.email);
        required(path.password);
        required(path.captcha);
    });

    readonly submitted = signal(false);
    readonly hidePassword = signal(true);

    async submit(event: SubmitEvent): Promise<void> {
        event.preventDefault();
        this.submitted.set(true);
        if (this.form().invalid()) {
            this.form().markAsTouched();
            return;
        }

        const { email, password, captcha } = this.model();
        const success = await this.store.login(email, password, captcha);
        if (!success) {
            return;
        }

        await this.router.navigate(["/cache/types"]);
    }

    togglePasswordVisibility(): void {
        this.hidePassword.update((value) => !value);
    }

    markForCheck(): void {
        this.change.markForCheck();
    }

    hasError(controlName: "email" | "password" | "captcha"): boolean {
        const field = this.form[controlName]();
        return field.invalid() && (field.touched() || this.submitted());
    }
}
