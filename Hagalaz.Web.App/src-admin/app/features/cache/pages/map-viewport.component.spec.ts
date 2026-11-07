import { ApplicationRef } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { MapViewportComponent } from "./map-viewport.component";

describe("MapViewportComponent keyboard controls", () => {
    beforeEach(() => TestBed.configureTestingModule({ imports: [MapViewportComponent] }));
    afterEach(() => vi.restoreAllMocks());

    it("suppresses click jitter, passes through camera drags, and clears selection on Escape", () => {
        const fixture = TestBed.createComponent(MapViewportComponent);
        const page = fixture.componentInstance;
        const selected = { regionId: 258, placement: { id: 1, shape: 10, rotation: 0, plane: 0, x: 0, y: 0 }, startVertex: 0, endVertex: 3 };
        const stopPropagation = vi.fn();
        const pointer = (x: number) => ({ isPrimary: true, button: 0, pointerId: 1, clientX: x, clientY: 0, stopPropagation }) as unknown as PointerEvent;
        page.selectedObject.set(selected);
        page.startPick(pointer(0));
        page.movePick(pointer(2));
        expect(stopPropagation).toHaveBeenCalledOnce();
        page.movePick(pointer(20));
        page.finishPick(pointer(0));
        expect(stopPropagation).toHaveBeenCalledOnce();
        expect(page.selectedObject()).toBe(selected);
        page.key(new KeyboardEvent("keydown", { key: "Escape" }));
        expect(page.selectedObject()).toBeNull();
        fixture.destroy();
    });

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
