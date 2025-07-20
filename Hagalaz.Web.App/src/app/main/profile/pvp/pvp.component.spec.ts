import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { PvpComponent } from "./pvp.component";

describe("PvpComponent", () => {
    let component: PvpComponent;
    let fixture: ComponentFixture<PvpComponent>;

    beforeEach(() => {
        fixture = TestBed.createComponent(PvpComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
