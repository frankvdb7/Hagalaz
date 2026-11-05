import { ApplicationRef } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { MapViewportComponent } from "./map-viewport.component";

describe("MapViewportComponent keyboard controls", () => {
    beforeEach(() => TestBed.configureTestingModule({ imports: [MapViewportComponent] }));
    afterEach(() => vi.restoreAllMocks());

    it("offers keyboard camera movement without triggering browser scrolling", () => {
        const fixture = TestBed.createComponent(MapViewportComponent);
        const page = fixture.componentInstance;
        const orbit = vi.spyOn(page, "orbit"), pan = vi.spyOn(page, "pan"), zoom = vi.spyOn(page, "zoom"), reset = vi.spyOn(page, "reset");
        const arrow = new KeyboardEvent("keydown", { key: "ArrowLeft", cancelable: true });
        page.key(arrow);
        expect(arrow.defaultPrevented).toBe(true);
        expect(orbit).toHaveBeenCalledWith(-0.2, 0);
        page.key(new KeyboardEvent("keydown", { key: "ArrowUp", shiftKey: true }));
        expect(pan).toHaveBeenCalledWith(0, 1);
        page.key(new KeyboardEvent("keydown", { key: "+" }));
        page.key(new KeyboardEvent("keydown", { key: "-" }));
        expect(zoom.mock.calls).toEqual([[0.8], [1.25]]);
        page.key(new KeyboardEvent("keydown", { key: "Home" }));
        expect(reset).toHaveBeenCalledOnce();
        const ordinary = new KeyboardEvent("keydown", { key: "Tab", cancelable: true });
        page.key(ordinary);
        expect(ordinary.defaultPrevented).toBe(false);
        fixture.destroy();
    });

    it("does not create a canvas after destruction before asynchronous initialization", async () => {
        const fixture = TestBed.createComponent(MapViewportComponent);
        const element = fixture.nativeElement as HTMLElement;
        fixture.destroy();
        await TestBed.inject(ApplicationRef).whenStable();
        expect(element.querySelector("canvas")).toBeNull();
    });
});
