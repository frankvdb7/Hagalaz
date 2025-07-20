import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { OverviewComponent } from "./overview.component";

describe("OverviewComponent", () => {
    let component: OverviewComponent;
    let fixture: ComponentFixture<OverviewComponent>;

    beforeEach(() => {
        fixture = TestBed.createComponent(OverviewComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
