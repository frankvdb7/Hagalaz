import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { LauncherComponent } from "./launcher.component";

describe("LauncherComponent", () => {
    let component: LauncherComponent;
    let fixture: ComponentFixture<LauncherComponent>;

    beforeEach(() => {
        fixture = TestBed.createComponent(LauncherComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
