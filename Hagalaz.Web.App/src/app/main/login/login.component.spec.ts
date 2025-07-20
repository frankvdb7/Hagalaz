import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { LoginComponent } from "./login.component";
import { importProvidersFrom } from "@angular/core";
import { NgHcaptchaModule } from "ng-hcaptcha";

describe("LoginComponent", () => {
    let component: LoginComponent;
    let fixture: ComponentFixture<LoginComponent>;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                importProvidersFrom(
                    NgHcaptchaModule.forRoot({
                        siteKey: "10000000-ffff-ffff-ffff-000000000001",
                    })
                ),
            ],
        });
        fixture = TestBed.createComponent(LoginComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
