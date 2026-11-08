import { TestbedHarnessEnvironment } from "@angular/cdk/testing/testbed";
import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { Component, input, model, output } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { MatTabGroupHarness } from "@angular/material/tabs/testing";
import { By } from "@angular/platform-browser";
import { provideRouter, Router } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../../environments/environment";
import { MapViewportComponent } from "../map-viewport.component";
import { MapsDetailPageComponent } from "./maps-detail.page";
import { CacheObjectsService } from "../../services/cache-objects.service";
import { required } from "../../../../../testing/required";

@Component({ selector: "cache-map-viewport", template: "<ng-content />" })
class ViewportBoundary {
    readonly geometry = input<unknown>();
    readonly textures = input<unknown>();
    readonly selectionKey = input<string>();
    readonly regionAtmosphere = input<unknown>(null);
    readonly environmentFaces = input<unknown>(null);
    readonly useRegionAtmosphere = model(true);
    readonly selectedObject = model<unknown>(null);
    readonly objectDrop = output<{ id: number; x: number; z: number }>();
}
const base = `${environment.cacheApiUrl}types/maps/`;
const region = (id: number) => ({ id, regionX: id >> 8, regionY: id & 255, terrainStatus: "absent", landscapeStatus: "absent", planes: [], objects: [], floors: [], warnings: [] });

describe("MapsDetailPageComponent", () => {
    it("requests appearance for an unused catalog object dropped on a region tile", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush(region(258));
        page.selectCatalogObject({ id: 99, name: "Fence", shape: 10 });
        const viewport = required(harness.routeDebugElement, "the routed page").query(By.directive(ViewportBoundary)).componentInstance as ViewportBoundary;
        viewport.objectDrop.emit({ id: 99, x: 3 * 512 + 100, z: 6 * 512 });
        harness.detectChanges();
        const batch = http.expectOne(`${environment.cacheApiUrl}types/objects/render-appearances`);
        expect(batch.request.body).toEqual([{ id: 99, shape: 10, rotation: 0 }]);
        expect(page.edits()?.additions).toEqual([{ regionId: 258, placement: { id: 99, shape: 10, rotation: 0, x: 3, y: 6, plane: 0 } }]);
        batch.flush([{ id: 99, shape: 10, rotation: 0, status: 404, appearance: null }]);
        http.expectNone(() => true);
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
        const viewport = required(harness.routeDebugElement, "the routed page").query(By.directive(ViewportBoundary)).componentInstance as ViewportBoundary;
        const key = viewport.selectionKey();
        expect(viewport.regionAtmosphere()).toEqual(atmosphere);
        expect(viewport.environmentFaces()).toBeNull();
        expect(viewport.useRegionAtmosphere()).toBe(true);
        viewport.useRegionAtmosphere.set(false);
        harness.detectChanges();
        expect(page.useRegionAtmosphere()).toBe(false);
        expect(viewport.selectionKey()).toBe(key);
        http.expectNone(() => true);
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
        expect(page.loadingText()).toContain("Appearances 0/1");
        expect(harness.routeNativeElement?.textContent).toContain("Appearances 0/1");
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
            { provide: CacheObjectsService, useValue: { catalogResource: () => ({ hasValue: () => false, isLoading: () => false, error: () => undefined }) } },
        ] });
        TestBed.overrideComponent(MapsDetailPageComponent, { remove: { imports: [MapViewportComponent] }, add: { imports: [ViewportBoundary] } });
        http = TestBed.inject(HttpTestingController);
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
        const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush(region(258));
        page.dimensions.width().value.set(2);
        await page.apply();
        harness.detectChanges();
        const left = http.expectOne(`${base}258/render`);
        expect(left.request.body.xteaKeys).toBeUndefined();
        left.flush(region(258));
        http.expectOne(`${base}514/render`).flush(region(514));
        expect(TestBed.inject(Router).url).toBe("/cache/types/maps/258?width=2");
        page.togglePlane(1, true);
        page.landscape.set(false);
        http.expectNone(() => true);
    });

    it("navigates to a region entered in the editor and preserves table context", async () => {
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl("/cache/types/maps/258?kind=maps&offset=25", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush(region(258));
        const root = required(harness.routeNativeElement, "the routed page");
        const loader = TestbedHarnessEnvironment.loader(harness.fixture);
        const assets = await loader.getHarness(MatTabGroupHarness.with({ selector: ".asset-tabs" }));
        await assets.selectTab({ label: /Textures/ });
        expect(root.textContent).toContain("Search textures by ID");
        const inspector = await loader.getHarness(MatTabGroupHarness.with({ selector: ".inspector-tabs" }));
        await inspector.selectTab({ label: /Tools/ });
        required(root.querySelector<HTMLButtonElement>('button[aria-haspopup="dialog"]'), "the dialog button").click();
        harness.detectChanges();
        const dialog = required(document.querySelector<HTMLFormElement>('form[role="dialog"]'), "the dialog form");
        const input = required(dialog.querySelector<HTMLInputElement>('input[type="number"]'), "the number input");
        input.value = "259";
        input.dispatchEvent(new Event("input", { bubbles: true }));
        dialog.dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
        await harness.fixture.whenStable();
        http.expectOne(`${base}259/render`).flush(region(259));
        expect(TestBed.inject(Router).url).toBe("/cache/types/maps/259?kind=maps&offset=25");
    });

    it("rejects wrapping detail links before requesting render data", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/65535?height=2", MapsDetailPageComponent);
        expect(page.selectedIds()).toEqual([]);
        expect(page.store.warnings()[0]).toContain("valid region rectangle");
        http.expectNone(() => true);
    });
});
