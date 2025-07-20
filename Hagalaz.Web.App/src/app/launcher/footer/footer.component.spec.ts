import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { LauncherFooterComponent } from "./footer.component";

describe("LauncherFooterComponent", () => {
    let component: LauncherFooterComponent;
    let fixture: ComponentFixture<LauncherFooterComponent>;

    beforeEach(() => {
        fixture = TestBed.createComponent(LauncherFooterComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
