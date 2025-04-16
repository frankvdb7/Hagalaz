import { Component, ChangeDetectionStrategy, inject, input, output } from "@angular/core";
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, UntypedFormBuilder, UntypedFormGroup, Validators } from "@angular/forms";
import { MatButton } from "@angular/material/button";
import { MatError, MatFormField, MatLabel, MatSuffix } from "@angular/material/form-field";
import { MatInput } from "@angular/material/input";
import { RouterLink } from "@angular/router";
import { MatCheckbox } from "@angular/material/checkbox";
import { MatIcon } from "@angular/material/icon";

export interface RegisterFormModel {
    email: string;
    password: string;
}

@Component({
    selector: "app-register-form",
    templateUrl: "./register-form.component.html",
    styleUrls: ["./register-form.component.scss"],
    imports: [MatButton, MatInput, MatFormField, MatLabel, MatError, RouterLink, MatCheckbox, FormsModule, ReactiveFormsModule, MatIcon, MatSuffix],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterFormComponent {
    private formBuilder = inject(FormBuilder);

    readonly error = input<unknown>();
    readonly registerSubmit = output<RegisterFormModel>();

    readonly form = this.formBuilder.group({
        email: ["", [Validators.required, Validators.email]],
        password: ["", Validators.required],
        passwordConfirm: ["", Validators.required],
        accepted: ["", Validators.requiredTrue],
    });

    onSubmit() {
        if (!this.form.valid) {
            return;
        }
        //this.registerSubmit.emit(this.form.value);
    }
}
