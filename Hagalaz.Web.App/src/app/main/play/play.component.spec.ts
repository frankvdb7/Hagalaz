import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { PlayComponent } from "./play.component";

describe("PlayComponent", () => {
    let component: PlayComponent;
    let fixture: ComponentFixture<PlayComponent>;

    beforeEach(() => {
        fixture = TestBed.createComponent(PlayComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
