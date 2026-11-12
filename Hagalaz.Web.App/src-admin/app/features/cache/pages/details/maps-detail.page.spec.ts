import type { MapPlacement } from "../../services/map-render.models";
import { ApplicationRef } from "@angular/core";
import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { MatDialog, type MatDialogRef } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import { By } from "@angular/platform-browser";
import { provideRouter } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { of } from "rxjs";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../../environments/environment";
import { CachePendingChangesService } from "../../cache-pending-changes.service";
import { MapLoadStatusComponent } from "../map-load-status.component";
import { MapScenePanelComponent } from "../map-scene-panel.component";
import { MapViewportComponent } from "../map-viewport.component";
import { projectileObjectRgb, walkableRgb } from "../map-walkability";
import { MapsDetailPageComponent } from "./maps-detail.page";
import { CacheObjectsService } from "../../services/cache-objects.service";
import { required } from "../../../../../testing/required";

const base = `${environment.cacheApiUrl}types/maps/`;
const worldMapUrl = `${base}worldmap/refresh`;
const region = (id: number) => ({ id, regionX: id >> 8, regionY: id & 255, terrainStatus: "absent", landscapeStatus: "absent", planes: [], objects: [], floors: [], warnings: [] });

describe("MapsDetailPageComponent", () => {
    it("requests appearance for an unused catalog object dropped on a region tile", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush(region(258));
        page.selectedAsset.set({ id: 99, name: "Fence", shape: 10 });
        const viewport = required(harness.routeDebugElement, "the routed page").query(By.directive(MapViewportComponent)).componentInstance as MapViewportComponent;
        viewport.objectDrop.emit({ id: 99, x: 3 * 512 + 100, z: 6 * 512 });
        harness.detectChanges();
        const batch = http.expectOne(`${environment.cacheApiUrl}types/objects/render-appearances`);
        expect(batch.request.body).toEqual([{ id: 99, shape: 10, rotation: 0 }]);
        expect(page.edits()?.additions).toEqual([{ regionId: 258, placement: { id: 99, shape: 10, rotation: 0, x: 3, y: 6, plane: 0 } }]);
        batch.flush([{ id: 99, shape: 10, rotation: 0, status: 404, appearance: null }]);
        http.expectNone(() => true);
    });
    it("applies a gizmo drag as one edit and shows the plane it lands on", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush({ ...region(258), objects: [{ id: 8, shape: 10, rotation: 3, plane: 0, x: 2, y: 4 }] });
        harness.detectChanges();
        http.expectOne(`${environment.cacheApiUrl}types/objects/render-appearances`).flush([{ id: 8, shape: 10, rotation: 3, status: 404, appearance: null }]);
        const viewport = required(harness.routeDebugElement, "the routed page").query(By.directive(MapViewportComponent)).componentInstance as MapViewportComponent;
        const placement = page.store.regions()[0].objects[0];
        page.selectedObject.set({ regionId: 258, placement, startVertex: 0, endVertex: 0 });
        expect(page.planes()[1]).toBe(false);

        viewport.objectGizmo.emit({ x: 5, y: 6, plane: 1, turns: 2, scale: 1.5 });

        expect(page.edits()?.transforms.get(placement)).toMatchObject({ x: 5, y: 6, plane: 1, rotation: 1, scale: 1.5 });
        expect(page.planes()[1]).toBe(true);
        page.undo();
        expect(page.edits()).toBeUndefined();
        expect(page.canUndo()).toBe(false);
    });
    it.each([["select", null], ["move", "translate"], ["rotate", "rotate"], ["scale", "scale"], ["terrain", null]] as const)("offers the gizmo for the %s tool as %s", async (tool, mode) => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush(region(258));
        page.editTool.set(tool);
        expect(page.gizmoMode()).toBe(mode);
    });
    it("undoes one edit at a time and forgets history on discard", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush({ ...region(258), objects: [{ id: 8, shape: 10, rotation: 0, plane: 0, x: 0, y: 0 }] });
        harness.detectChanges();
        http.expectOne(`${environment.cacheApiUrl}types/objects/render-appearances`).flush([{ id: 8, shape: 10, rotation: 0, status: 404, appearance: null }]);
        const viewport = required(harness.routeDebugElement, "the routed page").query(By.directive(MapViewportComponent)).componentInstance as MapViewportComponent;
        const placement = page.store.regions()[0].objects[0];
        const select = () => page.selectedObject.set({ regionId: 258, placement, startVertex: 0, endVertex: 0 });
        select();
        expect(viewport.canUndo()).toBe(false);

        page.changeTransform("x", 9);
        page.resetEdits();
        harness.detectChanges();
        expect(viewport.canUndo()).toBe(false);

        select();
        page.changeTransform("x", 5);
        page.changeTransform("x", 5);
        page.changeTransform("y", 7);
        harness.detectChanges();
        expect(viewport.canUndo()).toBe(true);
        viewport.undo.emit();
        expect(page.edits()?.transforms.get(placement)).toMatchObject({ x: 5, y: 0 });
        viewport.undo.emit();
        expect(page.edits()).toBeUndefined();
        harness.detectChanges();
        expect(viewport.canUndo()).toBe(false);
    });
    describe("selecting a region", () => {
        const placementOf = { id: 8, shape: 10, rotation: 0, plane: 0, x: 0, y: 0 };
        const load = async () => {
            const harness = await RouterTestingHarness.create();
            const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
            http.expectOne(`${base}258/render`).flush({ ...region(258), terrainStatus: "available", landscapeStatus: "available", objects: [placementOf] });
            harness.detectChanges();
            http.expectOne(`${environment.cacheApiUrl}types/objects/render-appearances`).flush([{ id: 8, shape: 10, rotation: 0, status: 404, appearance: null }]);
            harness.detectChanges();
            return { harness, page, root: required(harness.routeNativeElement, "the routed page") };
        };
        const inspector = (root: HTMLElement) => root.querySelector(".region-inspector")?.textContent ?? "";

        it("shows the region in the Inspector and clears an object selection", async () => {
            const { harness, page, root } = await load();
            page.selectedObject.set({ regionId: 258, placement: page.store.regions()[0].objects[0], startVertex: 0, endVertex: 0 });
            page.selectScene("region");
            await harness.fixture.whenStable();
            harness.detectChanges();
            expect(page.selectedObject()).toBeNull();
            expect(inspector(root)).toContain("Region 258");
            expect(inspector(root)).toContain("Coordinates");
            expect(inspector(root)).toContain("1, 2");
            expect(inspector(root)).toContain("Available");
        });

        it("replaces the region when an object is selected or the selection is cleared", async () => {
            const { page } = await load();
            page.selectScene("region");
            page.selectObject({ regionId: 258, placement: page.store.regions()[0].objects[0], startVertex: 0, endVertex: 0 });
            expect(page.sceneSelection()).toBeNull();
            page.selectScene("terrain");
            page.selectObject(null);
            expect(page.sceneSelection()).toBeNull();
        });

        it("shows terrain details when the terrain row is selected", async () => {
            const { harness, page, root } = await load();
            page.selectScene("terrain");
            await harness.fixture.whenStable();
            harness.detectChanges();
            expect(inspector(root)).toContain("Terrain 258");
            expect(inspector(root)).toContain("Status");
            expect(inspector(root)).not.toContain("World origin");
        });

        it("leaves the terrain tools so the region can be seen", async () => {
            const { harness, page } = await load();
            page.chooseTool("terrain");
            http.expectOne(`${base}floors`).flush([{ id: 2, overlay: false, rgb: 1, textureId: -1, secondaryRgb: null, textureScale: 512 }]);
            page.selectScene("region");
            harness.detectChanges();
            expect(page.editTool()).toBe("select");
        });
    });
    describe("editing terrain", () => {
        const planes = () => Array.from({ length: 4 }, () => ({ cornerHeights: Array(4225).fill(-640), flags: Array(4096).fill(0), underlays: Array(4096).fill(1), overlays: Array(4096).fill(0), overlayShapes: Array(4096).fill(0), overlayRotations: Array(4096).fill(0) }));
        const loadTerrain = async (objects: readonly MapPlacement[] = []) => {
            const harness = await RouterTestingHarness.create();
            const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
            http.expectOne(`${base}258/render`).flush({ ...region(258), terrainStatus: "available", landscapeStatus: "available", planes: planes(), objects });
            harness.detectChanges();
            page.brush.update(brush => ({ ...brush, size: 1 }));
            const viewport = required(harness.routeDebugElement, "the routed page").query(By.directive(MapViewportComponent)).componentInstance as MapViewportComponent;
            return { harness, page, viewport };
        };
        const flushFloors = () => http.expectOne(`${base}floors`).flush([{ id: 2, overlay: false, rgb: 1, textureId: -1, secondaryRgb: null, textureScale: 512 }]);

        it("loads floors and turns the brush on with the terrain tool", async () => {
            const { harness, page, viewport } = await loadTerrain();
            page.chooseTool("terrain");
            flushFloors();
            harness.detectChanges();
            expect(viewport.brushActive()).toBe(true);
            expect(required(harness.routeNativeElement, "the routed page").querySelector("cache-map-terrain-panel")).not.toBeNull();
            page.chooseTool("select");
            harness.detectChanges();
            expect(viewport.brushActive()).toBe(false);
        });

        it("inspects the clicked tile across hover and draft changes, and clears it when its plane is hidden", async () => {
            const { harness, page, viewport } = await loadTerrain();
            page.chooseTool("select");
            harness.detectChanges();
            expect(viewport.selectTerrainTiles()).toBe(true);
            viewport.tilePick.emit({ x: 550, z: 700, plane: 0 });
            viewport.brushHover.emit({ x: 1024, z: 1024, plane: 0 });
            harness.detectChanges();
            const host = required(harness.routeNativeElement, "the routed page");
            const row = (label: string) => Array.from(host.querySelectorAll("cache-map-region-details dt"))
                .find(cell => cell.textContent?.trim() === label)?.nextElementSibling?.textContent?.trim();
            expect(row("World tile")).toBe("65, 129");
            expect(row("Region tile")).toBe("1, 1");
            expect(row("Height")).toBe("640");
            expect(viewport.selectedTileOutline()).toEqual([
                512, 640, 512, 1024, 640, 512, 1024, 640, 1024, 512, 640, 1024,
            ]);
            page.changeBrush({ ...page.brush(), tool: "set-flag", flag: 1 });
            viewport.brushStroke.emit([{ x: 512, z: 512 }]);
            harness.detectChanges();
            expect(row("Flags")).toBe("1 (Blocked)");
            viewport.undo.emit();
            harness.detectChanges();
            expect(row("Flags")).toBe("0");
            page.togglePlane(0, false);
            harness.detectChanges();
            expect(row("World tile")).toBeUndefined();
            expect(viewport.selectedTileOutline()).toBeNull();
            page.togglePlane(0, true);
            harness.detectChanges();
            expect(row("World tile")).toBeUndefined();
        });

        it("commits one undo step per stroke and undoes it whole", async () => {
            const { harness, page, viewport } = await loadTerrain();
            viewport.brushStroke.emit([{ x: 512, z: 512 }, { x: 2048, z: 512 }]);
            harness.detectChanges();
            expect(page.edits()?.terrain?.get(258)?.size).toBe(4);
            expect(viewport.canUndo()).toBe(true);
            expect(viewport.canSave()).toBe(true);
            viewport.undo.emit();
            harness.detectChanges();
            expect(page.edits()).toBeUndefined();
            expect(viewport.canUndo()).toBe(false);
        });

        it("outlines the tile under a dragged object until the drag ends", async () => {
            const { harness, viewport } = await loadTerrain();
            viewport.objectDragHover.emit({ x: 512, z: 512 });
            harness.detectChanges();
            expect(viewport.dropOutline()).toHaveLength(12);
            viewport.objectDragHover.emit(null);
            harness.detectChanges();
            expect(viewport.dropOutline()).toBeNull();
        });

        it("sizes the drop outline to the dragged object's footprint", async () => {
            vi.spyOn(TestBed.inject(CacheObjectsService), "definitionResource").mockImplementation(((id: () => number | null) => ({ hasValue: () => id() === 99, value: () => ({ sizeX: 2, sizeY: 3 }), isLoading: () => false, error: () => undefined })) as never);
            const { harness, page, viewport } = await loadTerrain();
            page.selectedAsset.set({ id: 99, name: "Fence", shape: 10 });
            viewport.objectDragHover.emit({ x: 512, z: 512 });
            harness.detectChanges();
            expect(viewport.dropOutline()).toHaveLength((2 + 3) * 2 * 3);
        });

        it("keeps terrain edits when objects are edited", async () => {
            const { page, viewport } = await loadTerrain();
            viewport.brushStroke.emit([{ x: 512, z: 512 }]);
            const terrain = page.edits()?.terrain;
            page.selectedAsset.set({ id: 99, name: "Fence", shape: 10 });
            page.placeDroppedObject({ id: 99, x: 512, z: 512 });
            expect(page.edits()?.terrain).toBe(terrain);
            http.match(`${environment.cacheApiUrl}types/objects/render-appearances`).forEach(batch => { batch.flush([]); });
        });

        it("shows a raised plane to the scene while the stored region stays unchanged", async () => {
            const { harness, page, viewport } = await loadTerrain();
            viewport.brushStroke.emit([{ x: 512, z: 512 }]);
            harness.detectChanges();
            expect(viewport.geometry()).toBe(page.scene().geometry);
            expect(page.store.regions()[0].planes[0].cornerHeights[65 + 1]).toBe(-640);
            expect(page.edits()?.terrain?.get(258)?.get((1 << 6) | 1)?.height).toBe(22);
        });

        it("keeps walkability off until asked, then follows flag strokes and undo without rebuilding the scene geometry", async () => {
            const { harness, page, viewport } = await loadTerrain();
            expect(page.walkability()).toBe(false);
            expect(viewport.tileOverlay()).toBeNull();
            const terrain = page.scene().geometry?.terrain;
            page.walkability.set(true);
            harness.detectChanges();
            expect(viewport.tileOverlay()?.rgb.filter(rgb => rgb === walkableRgb)).toHaveLength(4096);
            expect(page.scene().geometry?.terrain).toBe(terrain);
            page.changeBrush({ ...page.brush(), tool: "set-flag", flag: 1 });
            viewport.brushStroke.emit([{ x: 512, z: 512 }, { x: 1536, z: 512 }]);
            harness.detectChanges();
            expect(viewport.tileOverlay()?.rgb.filter(rgb => rgb === 0xff2d2d)).toHaveLength(3);
            expect(viewport.tileOverlay()?.rgb.filter(rgb => rgb === walkableRgb)).toHaveLength(4093);
            expect(page.scene().geometry?.terrain).toBe(terrain);
            page.togglePlane(0, false);
            harness.detectChanges();
            expect(viewport.tileOverlay()).toBeNull();
            page.togglePlane(0, true);
            viewport.undo.emit();
            harness.detectChanges();
            expect(viewport.tileOverlay()?.rgb.filter(rgb => rgb === walkableRgb)).toHaveLength(4096);
            page.walkability.set(false);
            harness.detectChanges();
            expect(viewport.tileOverlay()).toBeNull();
        });

        it("shows both collision types without rebuilding geometry and keeps terrain visible when a definition batch fails", async () => {
            const { harness, page, viewport } = await loadTerrain();
            const solid = { sizeX: 1, sizeY: 1, clipType: 2, solid: true, gateway: false };
            const collisionUrl = `${environment.cacheApiUrl}types/objects/collision`;
            page.changeBrush({ ...page.brush(), tool: "set-flag", flag: 1 });
            viewport.brushStroke.emit([{ x: 512, z: 512 }]);
            const drop = (id: number, x: number) => {
                page.selectedAsset.set({ id, name: "Object", shape: 10 });
                viewport.objectDrop.emit({ id, x: x * 512, z: 6 * 512 });
                harness.detectChanges();
                http.expectOne(`${environment.cacheApiUrl}types/objects/render-appearances`).flush([{ id, shape: 10, rotation: 0, status: 404, appearance: null }]);
            };
            drop(8, 3);
            page.walkability.set(true);
            harness.detectChanges();
            http.expectOne(collisionUrl).flush([{ id: 8, status: "ok", collision: solid }]);
            await TestBed.inject(ApplicationRef).whenStable();
            harness.detectChanges();
            expect(viewport.tileOverlay()?.rgb).toContain(0xff2d2d);
            expect(viewport.tileOverlay()?.rgb).toContain(projectileObjectRgb);

            drop(9, 4);
            http.expectOne(collisionUrl).flush(null, { status: 500, statusText: "Server Error" });
            await TestBed.inject(ApplicationRef).whenStable();
            harness.detectChanges();
            expect(page.walkabilityLabel()).toContain("missing object data");
            expect(viewport.tileOverlay()?.rgb).toContain(0xff2d2d);
            expect(viewport.tileOverlay()?.rgb.filter(rgb => rgb === projectileObjectRgb)).toHaveLength(1);
        });

        it("shows world tile coordinates on hover and adds collision details while walkability is on", async () => {
            const wall = { id: 8, shape: 0, rotation: 1, plane: 0, x: 1, y: 1 };
            const gate = { id: 9, shape: 10, rotation: 0, plane: 0, x: 3, y: 3 };
            const { harness, page, viewport } = await loadTerrain([wall, gate]);
            http.expectOne(`${environment.cacheApiUrl}types/objects/render-appearances`).flush(
                [wall, gate].map(({ id, shape, rotation }) => ({ id, shape, rotation, status: 404, appearance: null })),
            );
            page.changeBrush({ ...page.brush(), tool: "set-flag", flag: 1 });
            viewport.brushStroke.emit([{ x: 512, z: 512 }]);
            viewport.brushHover.emit({ x: 512, z: 512, plane: 0 });
            harness.detectChanges();
            const host: HTMLElement = required(harness.routeDebugElement, "the routed page").nativeElement;
            const readout = () => host.querySelector(".tile-readout")?.textContent?.trim();
            expect(readout()).toBe("Tile 65, 129 · Plane 0");

            page.walkability.set(true);
            harness.detectChanges();
            http.expectOne(`${environment.cacheApiUrl}types/objects/collision`).flush([
                { id: 8, status: "ok", collision: { sizeX: 1, sizeY: 1, clipType: 2, solid: true, gateway: false } },
                { id: 9, status: "ok", collision: { sizeX: 1, sizeY: 1, clipType: 0, solid: false, gateway: true } },
            ]);
            await TestBed.inject(ApplicationRef).whenStable();
            harness.detectChanges();
            expect(viewport.hoveredTile()).toEqual({
                x: 65, y: 129, plane: 0,
                collision: { plane: 0, bits: ["WallNorth", "BlockedNorth", "FloorBlock", "WallAllowRangeNorth"] },
            });

            viewport.brushHover.emit({ x: 3 * 512, z: 3 * 512, plane: 0 });
            harness.detectChanges();
            expect(viewport.hoveredTile()?.collision?.bits).toEqual(["ObjectTile"]);
            page.walkability.set(false);
            harness.detectChanges();
            expect(readout()).toBe("Tile 67, 131 · Plane 0");
            viewport.brushHover.emit(null);
            harness.detectChanges();
            expect(readout()).toBeUndefined();
        });

        it("shows one legend with movement and projectile clipping only while walkability is on", async () => {
            const { harness, page } = await loadTerrain();
            const host: HTMLElement = required(harness.routeDebugElement, "the routed page").nativeElement;
            const legend = () => host.querySelector(".map-stage cache-map-overlay-legend");
            const labels = () => [...(legend()?.querySelectorAll("li") ?? [])].map(item => item.textContent?.trim());
            expect(legend()).toBeNull();
            page.walkability.set(true);
            harness.detectChanges();
            expect(labels()).toEqual([
                "Walkable floor", "Blocked", "Bridge", "Roof", "Draw on plane 0", "Hidden on minimap",
                "Object or decoration (movement only)", "Wall (movement only)", "Object (movement + projectiles)",
                "Wall (movement + projectiles)", "Ignored under bridge",
            ]);
            expect(legend()?.querySelector(".swatch.edge")).not.toBeNull();
            expect([...legend()?.querySelectorAll<HTMLElement>(".swatch span") ?? []].map(swatch => swatch.style.backgroundColor))
                .toEqual(expect.arrayContaining(["rgb(255, 99, 216)", "rgb(101, 223, 255)"]));
            page.walkability.set(false);
            harness.detectChanges();
            expect(legend()).toBeNull();
        });

        it("lists walkability with the other layer options", async () => {
            const { page, viewport } = await loadTerrain();
            // The menu renders lazily, so its template is drawn here as opening it would.
            const view = required(viewport.layersMenu(), "the layers menu").templateRef.createEmbeddedView({});
            view.detectChanges();
            const panel = document.createElement("div");
            panel.append(...view.rootNodes as Node[]);
            const items = [...panel.querySelectorAll<HTMLButtonElement>("button[mat-menu-item]")];
            const labels = items.map(item => item.textContent?.replace(/check_box(_outline_blank)?/, "").trim());
            expect(labels.slice(-2)).toEqual(["Object shadows", "Clipping"]);
            const item = required(items.at(-1), "the walkability item");
            expect(item.getAttribute("aria-checked")).toBe("false");
            item.click();
            expect(page.walkability()).toBe(true);
            view.destroy();
        });

        it("saves a flag stroke with the running game world note", async () => {
            const { harness, page, viewport } = await loadTerrain();
            const open = vi.spyOn(TestBed.inject(MatDialog), "open").mockReturnValue({ afterClosed: () => of(true) } as MatDialogRef<unknown>);
            page.changeBrush({ ...page.brush(), tool: "set-flag", flag: 4 });
            viewport.brushStroke.emit([{ x: 512, z: 512 }]);
            const saving = page.saveChanges();
            await harness.fixture.whenStable();
            expect(open.mock.calls[0][1]?.data).toBe("Save placement changes to region 258? Tile flag changes reach a running game world only after it reloads the region. World map areas other than the main map are not updated.");
            const request = http.expectOne(`${base}258/terrain`);
            expect(request.request.body).toEqual({ tiles: [{ plane: 0, x: 1, y: 1, flags: 4 }] });
            request.flush({});
            (await vi.waitFor(() => http.expectOne(worldMapUrl))).flush({ refreshed: [258], unchanged: [], skipped: [] });
            await saving;
            const stored = planes();
            stored[0].flags[65] = 4;
            http.expectOne(`${base}258/render`).flush({ ...region(258), terrainStatus: "available", planes: stored });
            harness.detectChanges();
            expect(page.edits()).toBeUndefined();
            expect(page.store.regions()[0].planes[0].flags[65]).toBe(4);
        });

        it("saves terrain then objects and reloads", async () => {
            const { harness, page, viewport } = await loadTerrain();
            const open = vi.spyOn(TestBed.inject(MatDialog), "open").mockReturnValue({ afterClosed: () => of(true) } as MatDialogRef<unknown>);
            viewport.brushStroke.emit([{ x: 512, z: 512 }]);
            const saving = page.saveChanges();
            await harness.fixture.whenStable();
            expect(open.mock.calls[0][1]?.data).toBe("Save placement changes to region 258? World map areas other than the main map are not updated.");
            const request = http.expectOne(`${base}258/terrain`);
            expect(request.request.body).toEqual({ tiles: [{ plane: 0, x: 1, y: 1, height: 22 }] });
            request.flush({});
            const refresh = await vi.waitFor(() => http.expectOne(worldMapUrl));
            expect(refresh.request.body).toEqual({ regions: [258] });
            refresh.flush({ refreshed: [258], unchanged: [], skipped: [] });
            await saving;
            expect(page.edits()).toBeUndefined();
            http.expectOne(`${base}258/render`).flush({ ...region(258), terrainStatus: "available", planes: planes() });
        });

        it("previews a stroke while dragging and hands over to the edit on release", async () => {
            const { harness, page, viewport } = await loadTerrain();
            viewport.brushPreview.emit([{ x: 512, z: 512 }]);
            harness.detectChanges();
            expect(viewport.strokePreview()).not.toBeNull();
            expect(page.edits()).toBeUndefined();
            viewport.brushStroke.emit([{ x: 512, z: 512 }]);
            harness.detectChanges();
            expect(page.strokePoints()).toBeNull();
            expect(viewport.strokePreview()).toBeNull();
            expect(page.edits()).toBeDefined();
        });

        it("drops the preview without an edit when a stroke is cancelled", async () => {
            const { harness, page, viewport } = await loadTerrain();
            viewport.brushPreview.emit([{ x: 512, z: 512 }]);
            viewport.brushPreview.emit(null);
            harness.detectChanges();
            expect(viewport.strokePreview()).toBeNull();
            expect(page.edits()).toBeUndefined();
        });

        it("turns on a hidden plane when the brush moves to it", async () => {
            const { page } = await loadTerrain();
            page.changeBrush({ ...page.brush(), plane: 2 });
            expect(page.planes()[2]).toBe(true);
        });
    });
    describe("saving to the cache", () => {
        const source = { id: 8, shape: 10, rotation: 0, plane: 0, x: 0, y: 0 };
        const loadRegion = async () => {
            const harness = await RouterTestingHarness.create();
            const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
            http.expectOne(`${base}258/render`).flush({ ...region(258), landscapeStatus: "available", objects: [source] });
            harness.detectChanges();
            http.expectOne(`${environment.cacheApiUrl}types/objects/render-appearances`).flush([{ id: 8, shape: 10, rotation: 0, status: 404, appearance: null }]);
            page.selectedObject.set({ regionId: 258, placement: page.store.regions()[0].objects[0], startVertex: 0, endVertex: 0 });
            const viewport = required(harness.routeDebugElement, "the routed page").query(By.directive(MapViewportComponent)).componentInstance as MapViewportComponent;
            return { harness, page, viewport };
        };
        const answer = (confirmed: boolean) => vi.spyOn(TestBed.inject(MatDialog), "open").mockReturnValue({ afterClosed: () => of(confirmed) } as MatDialogRef<unknown>);

        it("offers saving only for changes the cache can store", async () => {
            const { harness, page, viewport } = await loadRegion();
            expect(viewport.canSave()).toBe(false);
            page.changeTransform("scale", 2);
            harness.detectChanges();
            expect(viewport.canSave()).toBe(false);
            // The viewport drops a selection it cannot draw, and this region has no terrain.
            page.selectedObject.set({ regionId: 258, placement: page.store.regions()[0].objects[0], startVertex: 0, endVertex: 0 });
            page.changeTransform("x", 5);
            harness.detectChanges();
            expect(viewport.canSave()).toBe(true);
        });

        it("writes nothing and keeps edits when the confirmation is cancelled", async () => {
            const { page } = await loadRegion();
            const open = answer(false);
            page.changeTransform("x", 5);
            await page.saveChanges();
            expect(open.mock.calls[0][1]?.data).toBe("Save placement changes to region 258? World map areas other than the main map are not updated.");
            http.expectNone(`${base}258/landscape`);
            expect(page.edits()).toBeDefined();
        });

        it("states that scale is not stored", async () => {
            const { page } = await loadRegion();
            const open = answer(false);
            page.changeTransform("x", 5);
            page.changeTransform("scale", 2);
            await page.saveChanges();
            expect(open.mock.calls[0][1]?.data).toContain("Scale changes are not stored.");
        });

        it("saves the region, clears edits and reloads from the cache", async () => {
            const { harness, page, viewport } = await loadRegion();
            answer(true);
            page.changeTransform("x", 5);
            const saving = page.saveChanges();
            await harness.fixture.whenStable();
            const request = http.expectOne(`${base}258/landscape`);
            expect(request.request.body).toEqual({ objects: [{ ...source, x: 5 }] });
            request.flush({ operation: "map-landscape-updated", id: 258, placements: 1 });
            (await vi.waitFor(() => http.expectOne(worldMapUrl))).flush({ refreshed: [258], unchanged: [], skipped: [] });
            await saving;
            expect(page.edits()).toBeUndefined();
            harness.detectChanges();
            expect(viewport.canUndo()).toBe(false);
            http.expectOne(`${base}258/render`).flush({ ...region(258), landscapeStatus: "available", objects: [{ ...source, x: 5 }] });
            harness.detectChanges();
            http.match(`${environment.cacheApiUrl}types/objects/render-appearances`).forEach(batch => { batch.flush([]); });
        });

        it("reloads saved regions even while scene assets are still loading", async () => {
            const { harness, page } = await loadRegion();
            answer(true);
            page.selectedAsset.set({ id: 99, name: "Fence", shape: 10 });
            page.placeDroppedObject({ id: 99, x: 512, z: 512 });
            harness.detectChanges();
            const pendingAssets = http.expectOne(`${environment.cacheApiUrl}types/objects/render-appearances`);
            expect(page.store.loading()).toBe(true);

            const saving = page.saveChanges();
            (await vi.waitFor(() => http.expectOne(`${base}258/landscape`))).flush({});
            (await vi.waitFor(() => http.expectOne(worldMapUrl))).flush({ refreshed: [258], unchanged: [], skipped: [] });
            await saving;

            expect(page.edits()).toBeUndefined();
            expect(pendingAssets.cancelled).toBe(true);
            http.expectOne(`${base}258/render`).flush({ ...region(258), landscapeStatus: "available", objects: [source] });
            harness.detectChanges();
            http.match(`${environment.cacheApiUrl}types/objects/render-appearances`).forEach(batch => { batch.flush([]); });
        });

        it("removes a placement from the scene tree and offers no toolbar add or remove", async () => {
            const { harness, page } = await loadRegion();
            const placement = page.store.regions()[0].objects[0];
            const panel = required(harness.routeDebugElement, "the routed page").query(By.directive(MapScenePanelComponent)).componentInstance as MapScenePanelComponent;
            expect(harness.routeNativeElement?.querySelector('.map-toolbar [aria-label="Remove object"], .map-toolbar [aria-label="Add object"]')).toBeNull();

            panel.placementRemoved.emit(placement);

            expect(page.edits()?.transforms.get(placement)).toBeNull();
            expect(page.selectedObject()).toBeNull();
        });

        it("asks before leaving a draft, including one the cache cannot store", async () => {
            const { page } = await loadRegion();
            const pendingChanges = TestBed.inject(CachePendingChangesService);
            await expect(pendingChanges.canLeave()).resolves.toBe(true);
            page.changeTransform("scale", 2);

            const leaving = pendingChanges.canLeave();
            TestBed.tick();
            TestBed.inject(MatDialog).openDialogs[0].close(false);

            await expect(leaving).resolves.toBe(false);
            expect(page.edits()).toBeDefined();
        });

        it("refuses to leave while a save is writing to the cache", async () => {
            const { page } = await loadRegion();
            answer(true);
            page.changeTransform("x", 5);
            const saving = page.saveChanges();
            const request = await vi.waitFor(() => http.expectOne(`${base}258/landscape`));

            await expect(TestBed.inject(CachePendingChangesService).canLeave()).resolves.toBe(false);

            request.flush({ detail: "Landscape is encrypted." }, { status: 422, statusText: "Unprocessable" });
            await saving;
        });

        it("shows the save as a pending state in the viewport", async () => {
            const { harness, page } = await loadRegion();
            answer(true);
            page.changeTransform("x", 5);
            const saving = page.saveChanges();
            const request = await vi.waitFor(() => http.expectOne(`${base}258/landscape`));
            harness.detectChanges();

            expect(harness.routeNativeElement?.textContent).toContain("Saving changes");

            request.flush({ detail: "Landscape is encrypted." }, { status: 422, statusText: "Unprocessable" });
            await saving;
        });

        it("keeps the draft and starts no load when a retry is declined", async () => {
            const { harness, page } = await loadRegion();
            page.changeTransform("x", 5);
            const status = required(harness.routeDebugElement, "the routed page").query(By.directive(MapLoadStatusComponent)).componentInstance as MapLoadStatusComponent;

            status.retry.emit();
            TestBed.tick();
            TestBed.inject(MatDialog).openDialogs[0].close(false);
            await harness.fixture.whenStable();

            expect(page.edits()).toBeDefined();
            http.expectNone(`${base}258/render`);
        });

        it("keeps edits and reports the failure", async () => {
            const { harness, page } = await loadRegion();
            answer(true);
            page.changeTransform("x", 5);
            const saving = page.saveChanges();
            await harness.fixture.whenStable();
            http.expectOne(`${base}258/landscape`).flush({ detail: "Landscape is encrypted." }, { status: 422, statusText: "Unprocessable" });
            await saving;
            harness.detectChanges();
            expect(page.edits()).toBeDefined();
            expect(required(harness.routeNativeElement, "the routed page").querySelector('[role="alert"]')?.textContent).toContain("Could not save region 258 objects. Landscape is encrypted.");
            http.expectNone(worldMapUrl);
        });

        it("reports a failed world map refresh without losing the saved edits", async () => {
            const { harness, page } = await loadRegion();
            answer(true);
            page.changeTransform("x", 5);
            const saving = page.saveChanges();
            await harness.fixture.whenStable();
            http.expectOne(`${base}258/landscape`).flush({});
            (await vi.waitFor(() => http.expectOne(worldMapUrl))).flush({ detail: "Unavailable." }, { status: 500, statusText: "Error" });
            await saving;
            expect(page.edits()).toBeUndefined();
            http.expectOne(`${base}258/render`).flush({ ...region(258), landscapeStatus: "available", objects: [{ ...source, x: 5 }] });
            harness.detectChanges();
            http.match(`${environment.cacheApiUrl}types/objects/render-appearances`).forEach(batch => { batch.flush([]); });
            expect(page.store.saveError()).toBeNull();
            expect(required(harness.routeNativeElement, "the routed page").querySelector('[role="alert"]')?.textContent).toContain("The world map was not updated. Unavailable.");
        });

        it("rebuilds the world map only after confirmation and reports skipped regions", async () => {
            const { harness, page, viewport } = await loadRegion();
            harness.detectChanges();
            expect(viewport.canRebuildWorldMap()).toBe(true);
            const open = answer(false);
            viewport.rebuildWorldMap.emit();
            await harness.fixture.whenStable();
            http.expectNone(worldMapUrl);
            expect(open.mock.calls[0][1]?.data).toBe("Rebuild the world map for region 258? World map areas other than the main map are not updated.");

            open.mockReturnValue({ afterClosed: () => of(true) } as MatDialogRef<unknown>);
            const notify = vi.spyOn(TestBed.inject(MatSnackBar), "open");
            const rebuilding = page.rebuildWorldMap();
            const request = await vi.waitFor(() => http.expectOne(worldMapUrl));
            expect(request.request.body).toEqual({ regions: [258] });
            request.flush({ refreshed: [], unchanged: [], skipped: [{ id: 258, reason: "Region is not on the main world map." }] });
            await rebuilding;
            expect(notify.mock.calls[0][0]).toBe("World map already up to date. Not updated: 258.");
        });
    });
    it("discards every edit and its undo history from the viewport menu", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush({ ...region(258), objects: [{ id: 8, shape: 10, rotation: 0, plane: 0, x: 0, y: 0 }] });
        harness.detectChanges();
        http.expectOne(`${environment.cacheApiUrl}types/objects/render-appearances`).flush([{ id: 8, shape: 10, rotation: 0, status: 404, appearance: null }]);
        const viewport = required(harness.routeDebugElement, "the routed page").query(By.directive(MapViewportComponent)).componentInstance as MapViewportComponent;
        const placement = page.store.regions()[0].objects[0];
        page.selectedObject.set({ regionId: 258, placement, startVertex: 0, endVertex: 0 });
        page.changeTransform("x", 5);
        page.changeTransform("y", 7);
        harness.detectChanges();
        expect(viewport.canUndo()).toBe(true);
        viewport.discardEdits.emit();
        harness.detectChanges();
        expect(page.edits()).toBeUndefined();
        expect(viewport.canUndo()).toBe(false);
    });
    it("drops an undone addition from the selection", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush(region(258));
        page.selectedAsset.set({ id: 99, name: "Fence", shape: 10 });
        page.placeDroppedObject({ id: 99, x: 512, z: 512 });
        harness.detectChanges();
        http.expectOne(`${environment.cacheApiUrl}types/objects/render-appearances`).flush([{ id: 99, shape: 10, rotation: 0, status: 404, appearance: null }]);
        const placement = page.edits()?.additions[0].placement;
        page.selectedObject.set({ regionId: 258, placement: required(placement, "the added placement"), startVertex: 0, endVertex: 0 });
        page.undo();
        expect(page.edits()).toBeUndefined();
        expect(page.selectedObject()).toBeNull();
    });
    it("requests the rotated appearance for a rotation edit", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush({ ...region(258), objects: [{ id: 8, shape: 10, rotation: 0, plane: 0, x: 0, y: 0 }] });
        harness.detectChanges();
        const part = (id: number) => ({ id, positions: [], indices: [], faceColors: null, warnings: [] });
        const appearance = (rotation: number, model: number) => ({ id: 8, shape: 10, rotation, sizeX: 1, sizeY: 1, decorDisplacement: 0, contourMode: 0, contourParameter: 0, parts: [part(model)], warnings: [] });
        http.expectOne(`${environment.cacheApiUrl}types/objects/render-appearances`).flush([{ id: 8, shape: 10, rotation: 0, status: 200, appearance: appearance(0, 40) }]);
        const placement = page.store.regions()[0].objects[0];
        page.selectedObject.set({ regionId: 258, placement, startVertex: 0, endVertex: 0 });
        page.edits.set({ transforms: new Map([[placement, { x: 0, y: 0, plane: 0, rotation: 1, scale: 1 }]]), additions: [] });
        harness.detectChanges();
        const batch = http.expectOne(`${environment.cacheApiUrl}types/objects/render-appearances`);
        expect(batch.request.body).toEqual([{ id: 8, shape: 10, rotation: 1 }]);
        // The viewport drops a selection it cannot draw, and this region has no terrain.
        page.selectedObject.set({ regionId: 258, placement, startVertex: 0, endVertex: 0 });
        // The inspector lists the source models until the rotated appearance arrives, then its models.
        expect(page.objectModels().map(model => model.id)).toEqual([40]);
        batch.flush([{ id: 8, shape: 10, rotation: 1, status: 200, appearance: appearance(1, 41) }]);
        expect(page.objectModels().map(model => model.id)).toEqual([41]);
        // Moving or scaling keeps the same appearance keys, so the loader is not rescanned.
        const setVisibility = vi.spyOn(page.store, "setVisibility");
        page.edits.set({ transforms: new Map([[placement, { x: 5, y: 0, plane: 0, rotation: 1, scale: 2 }]]), additions: [] });
        harness.detectChanges();
        expect(setVisibility).not.toHaveBeenCalled();
        http.expectNone(() => true);
    });
    it("switches lighting from the viewport without reloading the region or changing its camera key", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
        const atmosphere = { lightRgb: 0xddcebb, intensity: 1.3, positiveResponse: 2, negativeResponse: 0.36, directionX: -4, directionY: -16, directionZ: 1, skyRgb: 0x8da4c2, fogValue: 600, environmentFaces: null };
        http.expectOne(`${base}258/render`).flush({ ...region(258), atmosphere });
        harness.detectChanges();
        const viewport = required(harness.routeDebugElement, "the routed page").query(By.directive(MapViewportComponent)).componentInstance as MapViewportComponent;
        const key = viewport.selectionKey();
        expect(viewport.regionAtmosphere()).toEqual(atmosphere);
        expect(viewport.snap()).toBe(1);
        expect(viewport.layersMenu()).toBeDefined();
        expect(viewport.environmentFaces()).toBeNull();
        expect(viewport.useRegionAtmosphere()).toBe(true);
        viewport.useRegionAtmosphere.set(false);
        harness.detectChanges();
        expect(page.useRegionAtmosphere()).toBe(false);
        expect(viewport.selectionKey()).toBe(key);
        http.expectNone(() => true);
    });
    it("explains a failed region and reloads it on retry", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush({ detail: "Unavailable." }, { status: 500, statusText: "Error" });
        harness.detectChanges();
        expect(harness.routeNativeElement?.textContent).toContain("Region could not be loaded");
        expect(harness.routeNativeElement?.textContent).toContain("Region 258: Unavailable.");
        const status = required(harness.routeDebugElement, "the routed page").query(By.directive(MapLoadStatusComponent)).componentInstance as MapLoadStatusComponent;

        status.retry.emit();

        (await vi.waitFor(() => http.expectOne(`${base}258/render`))).flush(region(258));
        expect(page.store.progress().failures).toBe(0);
    });

    it("reports incremental progress and requests a newly visible plane without reloading its region", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush({ ...region(258), objects: [{ id: 8, shape: 10, rotation: 0, plane: 1, x: 0, y: 0 }] });
        page.togglePlane(1, true);
        harness.detectChanges();
        await harness.fixture.whenStable();
        const batch = http.expectOne(`${environment.cacheApiUrl}types/objects/render-appearances`);
        expect(batch.request.body).toEqual([{ id: 8, shape: 10, rotation: 0 }]);
        expect(page.store.progress()).toMatchObject({ appearancesSettled: 0, appearancesTotal: 1 });
        expect(harness.routeNativeElement?.textContent).toContain("Loading objects");
        expect(harness.routeNativeElement?.textContent).toContain("0 of 1");
        batch.flush([{ id: 8, shape: 10, rotation: 0, status: 404, appearance: null, problem: { title: "No model" } }]);
        harness.detectChanges();
        expect(page.store.loading()).toBe(false);
        expect(page.store.warnings()).toContain("Object 8, shape 10: No model");
        http.expectNone(`${base}258/render`);
    });
    let http: HttpTestingController;
    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [
            provideRouter([{ path: "cache/types/maps/:id", component: MapsDetailPageComponent }]),
            provideHttpClient(withXhr()), provideHttpClientTesting(),
            { provide: CacheObjectsService, useValue: { catalogResource: () => ({ hasValue: () => false, isLoading: () => false, error: () => undefined }), definitionResource: () => ({ hasValue: () => false, isLoading: () => false, error: () => undefined }) } },
        ] });
        http = TestBed.inject(HttpTestingController);
        // The viewport reports WebGL as unavailable; jsdom would also log that it has no canvas.
        vi.spyOn(HTMLCanvasElement.prototype, "getContext").mockReturnValue(null);
    });
    afterEach(() => http.verify());

    it("loads a direct region link without the removed controls", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/258?kind=maps&offset=25&limit=25", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush(region(258));
        expect(page.selectedIds()).toEqual([258]);
        expect(harness.routeNativeElement?.textContent).not.toContain("Back to Maps");
        expect(harness.routeNativeElement?.textContent).not.toContain("Region keys");
        expect(harness.routeNativeElement?.textContent).not.toContain("Scene warnings");
        http.expectNone(() => true);
    });

    it("loads adjacent regions without requiring keys", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/258?width=2", MapsDetailPageComponent);
        const left = http.expectOne(`${base}258/render`);
        expect(left.request.body.xteaKeys).toBeUndefined();
        left.flush(region(258));
        http.expectOne(`${base}514/render`).flush(region(514));
        page.togglePlane(1, true);
        page.landscape.set(false);
        http.expectNone(() => true);
    });

    it("rejects wrapping detail links before requesting render data", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/65535?height=2", MapsDetailPageComponent);
        expect(page.selectedIds()).toEqual([]);
        expect(page.store.warnings()[0]).toContain("valid region rectangle");
        http.expectNone(() => true);
    });
});
