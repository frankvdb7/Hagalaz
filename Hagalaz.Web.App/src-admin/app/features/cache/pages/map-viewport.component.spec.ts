import { ApplicationRef } from "@angular/core";
import { TestbedHarnessEnvironment } from "@angular/cdk/testing/testbed";
import { TestBed } from "@angular/core/testing";
import { MatMenuHarness } from "@angular/material/menu/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { mapObjectDragType, MapViewportComponent } from "./map-viewport.component";

describe("MapViewportComponent keyboard controls", () => {
    beforeEach(() => TestBed.configureTestingModule({ imports: [MapViewportComponent] }));
    afterEach(() => {
        vi.useRealTimers();
        vi.restoreAllMocks();
    });

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

    it("hands the tile overlay to the scene and removes it again", () => {
        const fixture = TestBed.createComponent(MapViewportComponent);
        const setTileOverlay = vi.fn();
        const scene = new Proxy({ setTileOverlay } as Record<string | symbol, unknown>, { get: (target, name) => target[name] ?? vi.fn() });
        (fixture.componentInstance as unknown as { scene: { set(value: unknown): void } }).scene.set(scene);
        const overlay = { positions: Array(18).fill(0), uvs: Array(12).fill(0), rgb: [0xff2d2d], textureId: null, opacity: 0.55 };
        fixture.detectChanges();
        expect(setTileOverlay).toHaveBeenLastCalledWith(null);
        fixture.componentRef.setInput("tileOverlay", overlay);
        fixture.detectChanges();
        expect(setTileOverlay).toHaveBeenLastCalledWith(overlay);
        fixture.componentRef.setInput("tileOverlay", null);
        fixture.detectChanges();
        expect(setTileOverlay).toHaveBeenLastCalledWith(null);
        fixture.destroy();
    });

    it("batches terrain hover raycasts, skips drags, and emits only when the hovered tile changes", () => {
        vi.useFakeTimers();
        const fixture = TestBed.createComponent(MapViewportComponent);
        const viewport = fixture.componentInstance;
        const pickSurface = vi.fn((x: number) => ({ x, z: 1024, vertex: 0 }));
        const scene = new Proxy({ pickSurface } as Record<string | symbol, unknown>, { get: (target, name) => target[name] ?? vi.fn() });
        (viewport as unknown as { scene: { set(value: unknown): void } }).scene.set(scene);
        fixture.componentRef.setInput("geometry", { terrain: { mapPlanes: [2] }, objects: null, placements: [] });
        const hovers: unknown[] = [];
        viewport.brushHover.subscribe(point => hovers.push(point));
        const move = (clientX: number, buttons = 0) => viewport.movePick(new PointerEvent("pointermove", { clientX, buttons }));
        move(500);
        move(520);
        expect(pickSurface).not.toHaveBeenCalled();
        vi.advanceTimersByTime(75);
        expect(pickSurface).toHaveBeenCalledExactlyOnceWith(520, 0);
        expect(hovers).toEqual([{ x: 512, z: 1024, plane: 2 }]);
        move(550);
        vi.advanceTimersByTime(75);
        expect(hovers).toHaveLength(1);
        move(1100);
        viewport.startPick(new PointerEvent("pointerdown", { buttons: 1 }));
        move(1300, 1);
        vi.advanceTimersByTime(75);
        expect(pickSurface).toHaveBeenCalledTimes(2);
        viewport.finishPick(new PointerEvent("pointerup", { clientX: 1300 }));
        vi.advanceTimersByTime(75);
        expect(hovers.at(-1)).toEqual({ x: 1024, z: 1024, plane: 2 });
        viewport.leaveBrush();
        expect(hovers.at(-1)).toBeNull();
        fixture.destroy();
    });

    it("picks terrain only on Select clicks that miss objects and clears on Escape", () => {
        vi.useFakeTimers();
        const fixture = TestBed.createComponent(MapViewportComponent);
        const viewport = fixture.componentInstance;
        const placement = { startVertex: 0, endVertex: 3 };
        const pickVertex = vi.fn<() => number | null>().mockReturnValueOnce(0).mockReturnValue(null);
        const pickSurface = vi.fn<() => { x: number; z: number; vertex: number } | null>()
            .mockReturnValueOnce({ x: 550, z: 700, vertex: 0 }).mockReturnValue(null);
        const scene = new Proxy({ pickVertex, pickSurface, placementGizmoDragging: false } as Record<string | symbol, unknown>, { get: (target, name) => target[name] ?? vi.fn() });
        (viewport as unknown as { scene: { set(value: unknown): void } }).scene.set(scene);
        fixture.componentRef.setInput("geometry", { terrain: { mapPlanes: [0] }, objects: null, placements: [placement] });
        fixture.componentRef.setInput("selectTerrainTiles", true);
        const picked = vi.fn();
        viewport.tilePick.subscribe(picked);
        const click = () => {
            const point = { pointerId: 1, isPrimary: true, clientX: 12, clientY: 34 };
            viewport.startPick(new PointerEvent("pointerdown", point));
            viewport.finishPick(new PointerEvent("pointerup", point));
        };
        click();
        expect(viewport.selectedObject()).toBe(placement);
        expect(pickSurface).not.toHaveBeenCalled();
        click();
        expect(viewport.selectedObject()).toBeNull();
        expect(pickSurface).toHaveBeenCalledExactlyOnceWith(12, 34);
        expect(picked).toHaveBeenLastCalledWith({ x: 550, z: 700, plane: 0 });
        click();
        expect(picked).toHaveBeenLastCalledWith(null);
        fixture.componentRef.setInput("selectTerrainTiles", false);
        click();
        expect(pickSurface).toHaveBeenCalledTimes(2);
        viewport.key(new KeyboardEvent("keydown", { key: "Escape" }));
        expect(picked).toHaveBeenCalledTimes(3);
        expect(picked).toHaveBeenLastCalledWith(null);
        fixture.destroy();
    });

    it("shows the selection and hovered tile in one overlay and preserves the selection when hover clears", () => {
        const fixture = TestBed.createComponent(MapViewportComponent);
        fixture.componentRef.setInput("objectTransform", { x: 3, y: 4, plane: 1, rotation: 0, scale: 1 });
        fixture.componentRef.setInput("hoveredTile", {
            x: 65, y: 129, plane: 0, collision: { plane: 0, bits: ["FloorBlock", "WallNorth"] },
        });
        fixture.detectChanges();
        const host: HTMLElement = fixture.nativeElement;
        const canvas = host.querySelector(".map-canvas");
        const overlay = host.querySelector(".map-selection-status");
        const readout = host.querySelector(".tile-readout");
        if (!canvas?.parentElement || !overlay || !readout) throw new Error("Missing map canvas or status overlay");
        expect(host.querySelectorAll(".map-selection-status")).toHaveLength(1);
        expect(overlay.parentElement).toBe(canvas.parentElement);
        expect(readout.parentElement).toBe(overlay);
        expect(readout.textContent).toContain("Tile 65, 129 · Plane 0");
        expect(overlay.querySelector('[role="status"]')?.textContent).toContain("Tile 3, 4 · Plane 1");
        expect(getComputedStyle(canvas.parentElement).position).toBe("relative");
        expect(getComputedStyle(overlay).position).toBe("absolute");
        expect(getComputedStyle(overlay).pointerEvents).toBe("none");
        fixture.componentRef.setInput("hoveredTile", null);
        fixture.detectChanges();
        expect(host.querySelector(".map-canvas")).toBe(canvas);
        expect(host.querySelector(".tile-readout")).toBeNull();
        expect(overlay.textContent).toContain("1 object selected");
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
        expect(page.environment()).toEqual({ skyRgb: null, fog: null, environmentFaces: null, sun: null });
        expect(page.fogActive()).toBe(false);

        fixture.componentRef.setInput("regionAtmosphere", atmosphere);
        expect(page.fogActive()).toBe(true);
        expect(page.environment()).toEqual({ skyRgb: 0x8da4c2, fog: { start: 10908, end: 14332 }, environmentFaces: null, sun: atmosphere });

        page.fog.set(false);
        expect(page.environment()).toEqual({ skyRgb: 0x8da4c2, fog: null, environmentFaces: null, sun: atmosphere });

        page.fog.set(true);
        page.useRegionAtmosphere.set(false);
        expect(page.fogActive()).toBe(false);
        expect(page.environment()).toEqual({ skyRgb: null, fog: null, environmentFaces: null, sun: null });
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
