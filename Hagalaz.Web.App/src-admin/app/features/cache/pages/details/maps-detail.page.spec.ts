import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { Component, input } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { provideRouter, Router } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { environment } from "../../../../../environments/environment";
import { MapViewportComponent } from "../map-viewport.component";
import { MapsDetailPageComponent } from "./maps-detail.page";

@Component({ selector: "cache-map-viewport", template: "" })
class ViewportBoundary { readonly geometry = input<unknown>(); readonly textures = input<unknown>(); readonly selectionKey = input<string>(); }
@Component({ template: "Map table" })
class TableBoundary {}
const base = `${environment.cacheApiUrl}types/maps/`;
const region = (id: number) => ({ id, regionX: id >> 8, regionY: id & 255, terrainStatus: "absent", landscapeStatus: "absent", planes: [], objects: [], floors: [], warnings: [] });

describe("MapsDetailPageComponent", () => {
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
        expect(page.warnings()).toContain("Object 8, shape 10: No model");
        http.expectNone(`${base}258/render`);
    });
    let http: HttpTestingController;
    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideRouter([{ path: "cache/types/maps/:id", component: MapsDetailPageComponent }, { path: "cache/types", component: TableBoundary }]), provideHttpClient(withXhr()), provideHttpClientTesting()] });
        TestBed.overrideComponent(MapsDetailPageComponent, { remove: { imports: [MapViewportComponent] }, add: { imports: [ViewportBoundary] } });
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => http.verify());

    it("loads a direct region link and restores table context through Back to Maps", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/258?kind=maps&offset=25&limit=25", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush(region(258));
        expect(page.selectedIds()).toEqual([258]);
        const link = harness.routeNativeElement?.querySelector('a[href*="/cache/types?"]') as HTMLAnchorElement;
        expect(link.href).toContain("offset=25");
        link.click();
        await harness.fixture.whenStable();
        expect(TestBed.inject(Router).url).toBe("/cache/types?kind=maps&offset=25&limit=25");
        http.expectNone(() => true);
    });

    it("loads adjacent regions and applies keys without putting them in the URL", async () => {
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl("/cache/types/maps/258", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush(region(258));
        page.dimensions.width().value.set(2);
        page.keyForm.text().value.set('{"258":[1,2,3,4]}');
        await page.apply();
        harness.detectChanges();
        const left = http.expectOne(`${base}258/render`);
        expect(left.request.body).toEqual({ xteaKeys: [1, 2, 3, 4] });
        left.flush(region(258));
        http.expectOne(`${base}514/render`).flush(region(514));
        expect(TestBed.inject(Router).url).toBe("/cache/types/maps/258?width=2");
        page.togglePlane(1, true);
        page.landscape.set(false);
        http.expectNone(() => true);
        page.keyForm.text().value.set('{"258":[1,2,3]}');
        await page.apply();
        expect(page.error()).toContain("four signed");
        http.expectNone(() => true);
    });

    it("navigates to a region entered in the editor and preserves table context", async () => {
        const harness = await RouterTestingHarness.create();
        await harness.navigateByUrl("/cache/types/maps/258?kind=maps&offset=25", MapsDetailPageComponent);
        http.expectOne(`${base}258/render`).flush(region(258));
        const root = harness.routeNativeElement!;
        const input = root.querySelector<HTMLInputElement>('input[type="number"]')!;
        input.value = "259";
        input.dispatchEvent(new Event("input", { bubbles: true }));
        root.querySelector("form")!.dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
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

