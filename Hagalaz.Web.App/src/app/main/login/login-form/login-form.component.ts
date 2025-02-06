import { Component, ChangeDetectionStrategy, ChangeDetectorRef, inject, input, output } from "@angular/core";
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, UntypedFormBuilder, UntypedFormGroup, Validators } from "@angular/forms";
import { MatButton } from "@angular/material/button";
import { MatError, MatFormField, MatLabel } from "@angular/material/form-field";
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
    imports: [MatFormField, MatIcon, MatLabel, MatError, NgHcaptchaModule, FormsModule, ReactiveFormsModule, MatInput, MatButton],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginFormComponent {
    private formBuilder = inject(FormBuilder);
    private change = inject(ChangeDetectorRef);

    get emailControl() {
        return this.form.get("email");
    }

    get passwordControl() {
        return this.form.get("password");
    }

    get captchaControl() {
        return this.form.get("captcha");
    }

    readonly error = input<unknown>();
    readonly loginSubmit = output<LoginFormModel>();

    submitted: boolean = false;

    readonly form = this.formBuilder.group({
        email: this.formBuilder.control("", [Validators.required, Validators.email]),
        password: this.formBuilder.control("", Validators.required),
        captcha: this.formBuilder.control("", Validators.required),
    });

    markForCheck() {
        this.change.markForCheck();
    }

    onSubmit() {
        this.submitted = true;
        if (!this.form.valid) {
            return;
        }
        this.loginSubmit.emit(this.form.value as LoginFormModel);
    }
}
