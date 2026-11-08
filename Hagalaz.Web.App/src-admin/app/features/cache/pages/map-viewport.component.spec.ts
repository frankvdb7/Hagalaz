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

    it("derives the region sky and opt-in client fog only while region atmosphere is active", () => {
        const fixture = TestBed.createComponent(MapViewportComponent);
        const page = fixture.componentInstance;
        const atmosphere = { lightRgb: 0xffffff, intensity: 1, positiveResponse: 1, negativeResponse: 1, directionX: 0, directionY: -1, directionZ: 0, skyRgb: 0x8da4c2, fogValue: 600, environmentFaces: null };
        page.fog.set(true);
        expect(page.environment()).toEqual({ skyRgb: null, fog: null, environmentFaces: null });
        expect(page.fogActive()).toBe(false);

        fixture.componentRef.setInput("regionAtmosphere", atmosphere);
        expect(page.fogActive()).toBe(true);
        expect(page.environment()).toEqual({ skyRgb: 0x8da4c2, fog: { start: 10908, end: 14332 }, environmentFaces: null });

        page.fog.set(false);
        expect(page.environment()).toEqual({ skyRgb: 0x8da4c2, fog: null, environmentFaces: null });

        page.fog.set(true);
        page.useRegionAtmosphere.set(false);
        expect(page.fogActive()).toBe(false);
        expect(page.environment()).toEqual({ skyRgb: null, fog: null, environmentFaces: null });
        fixture.destroy();
    });

    it("passes loaded environment-map faces through only while region atmosphere is active", () => {
        const fixture = TestBed.createComponent(MapViewportComponent);
        const page = fixture.componentInstance;
        const faces = [{ image: {} as ImageBitmap, alphaMode: 0 as const }];
        fixture.componentRef.setInput("environmentFaces", faces);
        expect(page.environment().environmentFaces).toBeNull();

        fixture.componentRef.setInput("regionAtmosphere", { lightRgb: 0xffffff, intensity: 1, positiveResponse: 1, negativeResponse: 1, directionX: 0, directionY: -1, directionZ: 0, skyRgb: 0xc8c0a8, fogValue: 0, environmentFaces: [1, 2, 3, 4, 5, 6] });
        expect(page.environment().environmentFaces).toBe(faces);

        page.useRegionAtmosphere.set(false);
        expect(page.environment().environmentFaces).toBeNull();
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
