import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { LoginFormComponent } from "./login-form.component";
import { importProvidersFrom, NO_ERRORS_SCHEMA } from "@angular/core";
import { NgHcaptchaModule } from "ng-hcaptcha";
import { ReactiveFormsModule } from "@angular/forms";

describe("LoginFormComponent", () => {
    let component: LoginFormComponent;
    let fixture: ComponentFixture<LoginFormComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [
                LoginFormComponent,
                ReactiveFormsModule
            ],
            providers: [
                importProvidersFrom(
                    NgHcaptchaModule.forRoot({
                        siteKey: "10000000-ffff-ffff-ffff-000000000001",
                    })
                )
            ],
            schemas: [NO_ERRORS_SCHEMA] // Ignore unknown elements and properties
        }).compileComponents();

        fixture = TestBed.createComponent(LoginFormComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
