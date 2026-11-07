import { TestBed } from "@angular/core/testing";
import { afterEach, describe, expect, it, vi } from "vitest";
import { MapObjectThumbnailComponent } from "./map-object-thumbnail.component";

describe("MapObjectThumbnailComponent", () => {
    afterEach(() => {
        vi.restoreAllMocks();
        vi.unstubAllGlobals();
    });

    it("draws after the card is visible and loaded, and repaints only when scene colours change", () => {
        let intersect!: (entries: { isIntersecting: boolean }[]) => void;
        const disconnect = vi.fn();
        vi.stubGlobal("IntersectionObserver", class {
            constructor(callback: typeof intersect) { intersect = callback; }
            observe = vi.fn();
            disconnect = disconnect;
        });
        const schedule = vi.fn((_draw: () => void) => 1);
        vi.stubGlobal("requestIdleCallback", schedule);
        vi.stubGlobal("cancelIdleCallback", vi.fn());
        const fill = vi.fn();
        const context = { clearRect: vi.fn(), beginPath: vi.fn(), moveTo: vi.fn(), lineTo: vi.fn(), fill, fillStyle: "" };
        vi.spyOn(HTMLCanvasElement.prototype, "getContext").mockReturnValue(context as unknown as CanvasRenderingContext2D);

        const fixture = TestBed.createComponent(MapObjectThumbnailComponent);
        const range = { regionId: 258, placement: { id: 1, shape: 10, rotation: 0, plane: 0, x: 0, y: 0 }, startVertex: 0, endVertex: 3 };
        const geometry = { positions: [0, 0, 0, 128, 0, 0, 0, 128, 0], indices: [0, 1, 2], placements: [range] };
        fixture.componentRef.setInput("geometry", geometry);
        fixture.componentRef.setInput("range", range);
        fixture.detectChanges();
        intersect([{ isIntersecting: true }]);
        fixture.detectChanges();
        expect(schedule).not.toHaveBeenCalled();
        fixture.componentRef.setInput("ready", true);
        fixture.detectChanges();
        schedule.mock.calls[0][0]();
        expect(fill).toHaveBeenCalledOnce();
        expect(fixture.componentInstance.painted()).toBe(true);
        fixture.componentRef.setInput("ready", false);
        fixture.componentRef.setInput("ready", true);
        fixture.detectChanges();
        expect(schedule).toHaveBeenCalledOnce();
        // Placement edits rebuild geometry without changing this object's colours: no repaint.
        fixture.componentRef.setInput("geometry", { ...geometry });
        fixture.componentRef.setInput("range", { ...range });
        fixture.detectChanges();
        expect(schedule).toHaveBeenCalledOnce();
        // A relit or re-textured scene changes the colour key and repaints with the new colours.
        fixture.componentRef.setInput("geometry", { ...geometry, colors: [0, 0, 0, 0, 0, 0, 0, 0, 0] });
        fixture.componentRef.setInput("colourKey", {});
        fixture.detectChanges();
        expect(schedule).toHaveBeenCalledTimes(2);
        schedule.mock.calls[1][0]();
        expect(context.fillStyle).toBe("rgba(0,0,0,1)");
        fixture.destroy();
        expect(disconnect).toHaveBeenCalledOnce();
    });
});
