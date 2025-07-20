import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { RegisterFormComponent } from "./register-form.component";

describe("RegisterFormComponent", () => {
    let component: RegisterFormComponent;
    let fixture: ComponentFixture<RegisterFormComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [RegisterFormComponent],
        }).compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(RegisterFormComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
