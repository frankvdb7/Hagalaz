import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { CardTitleComponent } from "./card-title.component";

describe("CardTitleComponent", () => {
    let component: CardTitleComponent;
    let fixture: ComponentFixture<CardTitleComponent>;

    beforeEach(() => {
        fixture = TestBed.createComponent(CardTitleComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
