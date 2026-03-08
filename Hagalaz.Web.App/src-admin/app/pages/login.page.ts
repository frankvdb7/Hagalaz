import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, signal } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { Router } from "@angular/router";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { NgHcaptchaModule } from "ng-hcaptcha";
import { AuthStore } from "../core/auth/auth.store";
import { environment } from "../../environments/environment";

@Component({
    standalone: true,
    selector: "admin-login-page",
    imports: [ReactiveFormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressBarModule, NgHcaptchaModule],
    templateUrl: "./login.page.html",
    styleUrl: "./login.page.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPageComponent {
    private readonly fb = inject(FormBuilder);
    private readonly change = inject(ChangeDetectorRef);
    readonly store = inject(AuthStore);
    private readonly router = inject(Router);

    readonly isDev = !environment.production;

    readonly form = this.fb.nonNullable.group({
        email: ["", [Validators.required, Validators.email]],
        password: ["", [Validators.required]],
        captcha: ["", [Validators.required]],
    });

    readonly submitted = signal(false);
    readonly hidePassword = signal(true);

    submit(): void {
        this.submitted.set(true);
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        const { email, password, captcha } = this.form.getRawValue();
        this.store.login({ email, password, captcha });
    }

    devLogin(): void {
        this.store.devLogin();
    }

    togglePasswordVisibility(): void {
        this.hidePassword.update((value) => !value);
    }

    markForCheck(): void {
        this.change.markForCheck();
    }

    hasError(controlName: "email" | "password" | "captcha"): boolean {
        const control = this.form.controls[controlName];
        return control.invalid && (control.touched || this.submitted());
    }
}
