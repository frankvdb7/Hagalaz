import { TestbedHarnessEnvironment } from "@angular/cdk/testing/testbed";
import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";

import { MatTabGroupHarness } from "@angular/material/tabs/testing";
import { By } from "@angular/platform-browser";
import { provideRouter, Router } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../../environments/environment";
import { PortalBreadcrumb } from "../../../../layout/portal-breadcrumb";
import { ItemsDetailPageComponent } from "./items-detail.page";

const text = (element: Element | null) => element?.textContent?.replace(/\s+/g, "").trim();
const baseUrl = `${environment.cacheApiUrl}types`;
const item = {
    id: 7,
    name: "Runic blade",
    value: 0,
    stackable: false,
    noted: false,
    membersOnly: true,
    equipSlot: 0,
    equipType: -1,
    renderAnimationId: 123,
    interfaceModelId: 42,
    noteId: 0,
    noteTemplateId: -1,
    lendId: 9,
    lendTemplateId: 10,
    boughtItemId: null,
    inventoryOptions: ["Wield", null, "Check", null, "Drop"],
    groundOptions: [null, null, "Take", null, null],
};

describe("Item Forge", () => {
    let http: HttpTestingController;
    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                PortalBreadcrumb,
                provideRouter([{ path: "cache/types/items/:id", component: ItemsDetailPageComponent }]),
                provideHttpClient(withXhr()),
                provideHttpClientTesting(),
            ],
        });
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => {
        TestBed.resetTestingModule();
        http.verify({ ignoreCancelled: true });
    });

    async function open(data: Record<string, unknown> = item, failure = false) {
        const opening = RouterTestingHarness.create("/cache/types/items/7");
        await vi.waitFor(() => {
            const request = http.expectOne(`${baseUrl}/items/7`);
            if (failure) request.flush({ detail: "Item unavailable" }, { status: 503, statusText: "Unavailable" });
            else request.flush(data);
        });
        const harness = await opening;
        await harness.fixture.whenStable();
        const loader = TestbedHarnessEnvironment.loader(harness.fixture);
        const page = harness.fixture.debugElement.query(By.directive(ItemsDetailPageComponent)).componentInstance as ItemsDetailPageComponent;
        return { harness, loader, page, element: harness.routeNativeElement as HTMLElement };
    }

    it("shows actual zero/false values and keeps the workspace read only", async () => {
        const { element, loader } = await open();
        expect(element.querySelector("h3")?.textContent).toBe("Runic blade");
        expect(text(element)).toContain("Cachebasevalue0");
        expect(text(element)).toContain("StackableNo");
        expect(text(element)).toContain("MembersonlyYes");
        expect(element.textContent).toContain("Market and trade prices");
        expect(element.querySelectorAll("img")).toHaveLength(0);
        expect(element.querySelector("cache-item-navigator")).toBeNull();
        expect(element.textContent).not.toContain("Save");
        expect(await (await loader.getHarness(MatTabGroupHarness)).getTabs()).toHaveLength(5);
    });

    it("preserves all ordered inventory and ground action slots", async () => {
        const { element, loader } = await open();
        await (await loader.getHarness(MatTabGroupHarness)).selectTab({ label: "Interactions" });
        await vi.waitFor(() => expect(element.querySelectorAll(".action-groups ol")).toHaveLength(2));
        const groups = element.querySelectorAll(".action-groups ol");
        expect([...groups[0].querySelectorAll("li")].map((slot) => text(slot))).toEqual(["1Wield", "2Emptyslot", "3Check", "4Emptyslot", "5Drop"]);
        expect([...groups[1].querySelectorAll("li")].map((slot) => text(slot))).toEqual(["1Emptyslot", "2Emptyslot", "3Take", "4Emptyslot", "5Emptyslot"]);
    });

    it("shows absent fields without converting them to false or zero", async () => {
        const { element, loader } = await open({ id: 7, name: "" });
        expect(element.textContent).toContain("Unnamed item");
        expect(text(element)).toContain("CachebasevalueNotsupplied");
        expect(text(element)).toContain("StackableNotsupplied");
        await (await loader.getHarness(MatTabGroupHarness)).selectTab({ label: "Interactions" });
        await vi.waitFor(() => expect(element.querySelector(".action-groups")).not.toBeNull());
        await vi.waitFor(() => expect(element.querySelectorAll(".action-groups li")).toHaveLength(2));
        expect(element.querySelector(".action-groups")?.textContent).toContain("Not supplied");
    });

    it("maps equipment identifiers and distinguishes absent sentinels", async () => {
        const { element, loader } = await open();
        await (await loader.getHarness(MatTabGroupHarness)).selectTab({ label: "Equipment" });
        await vi.waitFor(() => expect(text(element)).toContain("Equipslot0"));
        expect(text(element)).toContain("Equipslot0");
        expect(text(element)).toContain("EquiptypeNotset");
        expect(text(element)).toContain("RenderanimationID123");
        expect(element.textContent).toContain("Gameplay bonuses");
    });

    it("links only explicit relationships including item zero and navigates on the same route", async () => {
        const { element, loader, harness } = await open();
        await (await loader.getHarness(MatTabGroupHarness)).selectTab({ label: "Variants" });
        await vi.waitFor(() => expect(element.querySelectorAll(".definition-panel cache-item-fields a")).toHaveLength(3));
        const fields = element.querySelector(".definition-panel cache-item-fields");
        if (!fields) throw new Error("Expected relationship fields");
        expect([...fields.querySelectorAll("a")].map((link) => link.getAttribute("href"))).toEqual([
            "/cache/types/items/0",
            "/cache/types/items/9",
            "/cache/types/items/10",
        ]);
        expect(text(fields)).toContain("NotetemplateNotset");
        expect(text(fields)).toContain("BoughtitemNotsupplied");
        expect(text(fields)).toContain("BoughttemplateNotsupplied");
        (fields.querySelector("a") as HTMLAnchorElement).click();
        await vi.waitFor(() => http.expectOne(`${baseUrl}/items/0`).flush({ ...item, id: 0, name: "Linked item" }));
        await harness.fixture.whenStable();
        expect(TestBed.inject(Router).url).toBe("/cache/types/items/0");
        expect(element.textContent).toContain("Linked item");
    });

    it("shows the exact response in the raw tab", async () => {
        const { element, loader } = await open();
        await (await loader.getHarness(MatTabGroupHarness)).selectTab({ label: "Raw data" });
        await vi.waitFor(() => expect(JSON.parse(element.querySelector("pre")?.textContent || "null")).toEqual(item));
    });

    it("reports item failure, retries and shows loading inside its card", async () => {
        const { element, page, harness } = await open({}, true);
        expect(element.querySelector('[role="alert"]')?.textContent).toContain("Item unavailable");
        page.reload();
        TestBed.tick();
        expect(element.querySelector(".definition-panel mat-progress-bar")).not.toBeNull();
        http.expectOne(`${baseUrl}/items/7`).flush(item);
        await harness.fixture.whenStable();
        expect(element.querySelector('[role="alert"]')).toBeNull();
        expect(element.textContent).toContain("Runic blade");
    });

    it("rejects invalid and unsafe IDs without requesting a definition", async () => {
        const { harness, page } = await open();
        for (const id of ["invalid", "-1", "9007199254740992"]) {
            await harness.navigateByUrl(`/cache/types/items/${id}`);
            http.expectNone((request) => /\/items\/[^/]+$/.test(request.url));
            expect(harness.routeNativeElement?.textContent).toContain("Invalid item ID");
            expect(page.item()).toBeNull();
            page.reload();
        }
    });
});
