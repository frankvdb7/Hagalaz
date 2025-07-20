import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { HeaderNavComponent } from "./header-nav.component";

describe("HeaderNavComponent", () => {
    let component: HeaderNavComponent;
    let fixture: ComponentFixture<HeaderNavComponent>;

    beforeEach(() => {
        fixture = TestBed.createComponent(HeaderNavComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
