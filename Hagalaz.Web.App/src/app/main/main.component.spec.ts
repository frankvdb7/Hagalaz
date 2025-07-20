import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { MainComponent } from "./main.component";

describe("MainComponent", () => {
    let component: MainComponent;
    let fixture: ComponentFixture<MainComponent>;

    beforeEach(() => {
        fixture = TestBed.createComponent(MainComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
