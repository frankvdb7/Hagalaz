import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { TorchComponent } from "./torch.component";

describe("TorchComponent", () => {
    let component: TorchComponent;
    let fixture: ComponentFixture<TorchComponent>;

    beforeEach(() => {
        fixture = TestBed.createComponent(TorchComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
