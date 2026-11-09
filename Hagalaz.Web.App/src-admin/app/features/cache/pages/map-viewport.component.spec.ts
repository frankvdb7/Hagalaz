import { ApplicationRef } from "@angular/core";
import { TestbedHarnessEnvironment } from "@angular/cdk/testing/testbed";
import { TestBed } from "@angular/core/testing";
import { MatMenuHarness } from "@angular/material/menu/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { mapObjectDragType, MapViewportComponent } from "./map-viewport.component";

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

    describe("dragging an object over the viewport", () => {
        function setup(hit: { x: number; z: number } | null) {
            const fixture = TestBed.createComponent(MapViewportComponent);
            const page = fixture.componentInstance;
            const surface = { hit };
            // Rendering the viewport calls many scene setters; only picking matters here.
            const scene = new Proxy({ pickSurface: () => surface.hit } as Record<string | symbol, unknown>, { get: (target, name) => target[name] ?? vi.fn() });
            (page as unknown as { scene: { set(value: unknown): void } }).scene.set(scene);
            const frames: FrameRequestCallback[] = [];
            vi.spyOn(globalThis, "requestAnimationFrame").mockImplementation(callback => frames.push(callback));
            const hovers: unknown[] = [];
            page.objectDragHover.subscribe(point => hovers.push(point));
            const dataTransfer = { types: [mapObjectDragType], dropEffect: "", getData: () => "" };
            const event = { dataTransfer, preventDefault: vi.fn(), clientX: 1, clientY: 2, relatedTarget: null } as unknown as DragEvent;
            const flush = () => frames.splice(0).forEach(frame => { frame(0); });
            return { fixture, page, surface, hovers, dataTransfer, event, flush };
        }

        it("samples the ground once a frame and reports where the object would land", () => {
            const { fixture, page, hovers, dataTransfer, event, flush } = setup({ x: 512, z: 1024 });
            page.allowObjectDrop(event);
            page.allowObjectDrop(event);
            expect(event.preventDefault).toHaveBeenCalledTimes(2);
            expect(dataTransfer.dropEffect).toBe("copy");
            flush();
            expect(hovers).toEqual([{ x: 512, z: 1024 }]);
            fixture.destroy();
        });

        it("refuses the drop away from the terrain", () => {
            const { fixture, page, surface, hovers, dataTransfer, event, flush } = setup({ x: 512, z: 1024 });
            page.allowObjectDrop(event);
            flush();
            surface.hit = null;
            page.allowObjectDrop(event);
            flush();
            expect(hovers.at(-1)).toBeNull();
            page.allowObjectDrop(event);
            expect(dataTransfer.dropEffect).toBe("none");
            fixture.destroy();
        });

        it("clears the target when the drag leaves, but not between its own canvases", () => {
            const { fixture, page, hovers, event } = setup({ x: 512, z: 1024 });
            fixture.detectChanges();
            const canvas = fixture.nativeElement.querySelector(".map-canvas") as HTMLElement;
            const inner = canvas.appendChild(document.createElement("canvas"));
            page.leaveObjectDrop({ ...event, relatedTarget: inner } as unknown as DragEvent);
            expect(hovers).toEqual([]);
            page.leaveObjectDrop(event);
            expect(hovers).toEqual([null]);
            fixture.destroy();
        });

        it("ignores drags that are not objects", () => {
            const { fixture, page, event, dataTransfer } = setup(null);
            dataTransfer.types = ["text/plain"];
            page.allowObjectDrop(event);
            expect(event.preventDefault).not.toHaveBeenCalled();
            fixture.destroy();
        });
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

    it("shows and updates the point lights checkbox independently of the region atmosphere", async () => {
        const fixture = TestBed.createComponent(MapViewportComponent);
        const page = fixture.componentInstance;
        const field = { width: 1, height: 1, tiles: new Float32Array(16), lights: [{ x: 256, y: 128, z: 256, radius: 256, rgb: 0xffffff }] };
        expect(page.pointLightsActive()).toBe(false);
        fixture.componentRef.setInput("pointLights", field);
        page.useRegionAtmosphere.set(false);
        const view = await TestbedHarnessEnvironment.loader(fixture).getHarness(MatMenuHarness.with({ triggerText: /View/ }));
        await view.open();
        const [lighting] = await view.getItems({ text: /Lighting:/ });
        const submenu = (await lighting.getSubmenu())!;
        await submenu.open();
        const [pointLights] = await submenu.getItems({ text: /Point lights/ });
        expect(await pointLights.getText()).toContain("check_box");
        expect(await (await pointLights.host()).getAttribute("aria-checked")).toBe("true");
        await pointLights.click();
        expect(await pointLights.getText()).toContain("check_box_outline_blank");
        expect(await (await pointLights.host()).getAttribute("aria-checked")).toBe("false");
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
