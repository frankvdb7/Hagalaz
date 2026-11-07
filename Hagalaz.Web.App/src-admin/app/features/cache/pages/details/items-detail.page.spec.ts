import { MatSliderHarness } from "@angular/material/slider/testing";
import { MatInputHarness } from "@angular/material/input/testing";
import { MatExpansionPanelHarness } from "@angular/material/expansion/testing";
import { ItemSpriteThumbnailComponent } from "../item-sprite-thumbnail.component";
import { MatDialog } from "@angular/material/dialog";
import { CachePendingChangesService } from "../../cache-pending-changes.service";
import { TestbedHarnessEnvironment } from "@angular/cdk/testing/testbed";
import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";

import { MatTabGroupHarness } from "@angular/material/tabs/testing";
import { MatSelectHarness } from "@angular/material/select/testing";
import { MatSlideToggleHarness } from "@angular/material/slide-toggle/testing";
import { By } from "@angular/platform-browser";
import { provideRouter, Router } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../../environments/environment";
import { PortalBreadcrumb } from "../../../../layout/portal-breadcrumb";
import { ItemsDetailPageComponent } from "./items-detail.page";

import { Component, input } from "@angular/core";
import { CacheModelPreviewComponent } from "../cache-model-preview.component";

@Component({ selector: "cache-item-sprite-thumbnail", template: "" })
class ItemSpriteBoundary {
    readonly id = input.required<number>();
    readonly name = input("");
    readonly size = input(44);
    readonly revision = input<unknown>();
    readonly quantity = input<number>();
    readonly outline = input<number>();
    readonly shadow = input<string>();
}

@Component({ selector: "cache-model-preview", template: "" })
class ModelPreviewBoundary {
    readonly modelId = input<number | null>(null);
    readonly itemId = input<number | null>(null);
    readonly height = input(360);
}

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
        TestBed.overrideComponent(ItemsDetailPageComponent, {
            remove: { imports: [CacheModelPreviewComponent, ItemSpriteThumbnailComponent] },
            add: { imports: [ModelPreviewBoundary, ItemSpriteBoundary] },
        });
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => {
        TestBed.resetTestingModule();
        http.verify({ ignoreCancelled: true });
    });

    function previewSprite(harness: RouterTestingHarness): ItemSpriteBoundary {
        return harness.fixture.debugElement.query(By.css(".preview-image cache-item-sprite-thumbnail")).componentInstance as ItemSpriteBoundary;
    }

    async function open(data: Record<string, unknown> = item, failure = false) {
        const opening = RouterTestingHarness.create("/cache/types/items/7");
        await vi.waitFor(() => {
            const request = http.expectOne(`${baseUrl}/items/7/definition`);
            if (failure) request.flush({ detail: "Item unavailable" }, { status: 503, statusText: "Unavailable" });
            else request.flush(data);
        });
        const harness = await opening;
        await harness.fixture.whenStable();
        const loader = TestbedHarnessEnvironment.loader(harness.fixture);
        const page = harness.fixture.debugElement.query(By.directive(ItemsDetailPageComponent)).componentInstance as ItemsDetailPageComponent;
        return { harness, loader, page, element: harness.routeNativeElement as HTMLElement };
    }

    it("shows the reference editor with actual zero values and real preview wiring", async () => {
        const { element, loader, harness } = await open();
        const preview = harness.fixture.debugElement.query(By.directive(ModelPreviewBoundary)).componentInstance as ModelPreviewBoundary;
        expect(preview.modelId()).toBe(42);
        expect(preview.itemId()).toBe(7);
        expect(element.querySelector("h2")?.textContent).toBe("Runic blade");
        expect((element.querySelector("#item-value") as HTMLInputElement).value).toBe("0");
        expect(text(element)).toContain("Stackable:No");
        expect(text(element)).toContain("Membersonly:Yes");
        expect(element.textContent).toContain("market and trade prices");
        expect(element.querySelector("cache-item-sprite-thumbnail")).not.toBeNull();
        expect(element.querySelector(".technical-panel")).not.toBeNull();
        expect(element.querySelector(".save-action")?.hasAttribute("disabled")).toBe(true);
        expect(await (await loader.getHarness(MatTabGroupHarness)).getTabs()).toHaveLength(5);
    });

    it("edits through Material inputs and keeps derived fields readonly", async () => {
        const { loader, page } = await open();
        const id = await loader.getHarness(MatInputHarness.with({ selector: "#item-id" }));
        const description = await loader.getHarness(MatInputHarness.with({ selector: "#item-description" }));
        expect(await id.isReadonly()).toBe(true);
        expect(await description.isReadonly()).toBe(true);
        const name = await loader.getHarness(MatInputHarness.with({ selector: "#item-name" }));
        const value = await loader.getHarness(MatInputHarness.with({ selector: "#item-value" }));
        await name.setValue("Updated blade");
        await value.setValue("125");
        expect(page.draft()?.name).toBe("Updated blade");
        expect(page.draft()?.value).toBe(125);
        expect(page.canSave()).toBe(true);
        await name.setValue("");
        await name.blur();
        expect(page.canSave()).toBe(false);
    });

    it("reveals the existing model preview through the Material inspector", async () => {
        const { loader, harness } = await open();
        const inspector = await loader.getHarness(MatExpansionPanelHarness);
        expect(await inspector.isExpanded()).toBe(false);
        await inspector.expand();
        expect(await inspector.isExpanded()).toBe(true);
        const preview = harness.fixture.debugElement.query(By.directive(ModelPreviewBoundary)).componentInstance as ModelPreviewBoundary;
        expect(preview.modelId()).toBe(42);
        expect(preview.itemId()).toBe(7);
        await inspector.collapse();
        expect(await inspector.isExpanded()).toBe(false);
    });

    it("preserves all ordered inventory and ground action slots", async () => {
        const { element, loader } = await open();
        await (await loader.getHarness(MatTabGroupHarness)).selectTab({ label: /Interactions/ });
        await vi.waitFor(() => expect(element.querySelectorAll(".action-groups ol")).toHaveLength(2));
        const groups = element.querySelectorAll(".action-groups ol");
        expect([...groups[0].querySelectorAll("li")].map((slot) => text(slot))).toEqual(["1Wield", "2Emptyslot", "3Check", "4Emptyslot", "5Drop"]);
        expect([...groups[1].querySelectorAll("li")].map((slot) => text(slot))).toEqual(["1Emptyslot", "2Emptyslot", "3Take", "4Emptyslot", "5Emptyslot"]);
    });

    it("shows absent fields without converting them to false or zero", async () => {
        const { element, loader } = await open({ id: 7, name: "" });
        expect(element.textContent).toContain("Unnamed item");
        expect(text(element)).toContain("Basevalue:Notsupplied");
        expect(text(element)).toContain("Stackable:Notsupplied");
        await (await loader.getHarness(MatTabGroupHarness)).selectTab({ label: /Interactions/ });
        await vi.waitFor(() => expect(element.querySelector(".action-groups")).not.toBeNull());
        await vi.waitFor(() => expect(element.querySelectorAll(".action-groups li")).toHaveLength(2));
        expect(element.querySelector(".action-groups")?.textContent).toContain("Not supplied");
    });

    it("maps equipment identifiers and distinguishes absent sentinels", async () => {
        const { element, loader } = await open();
        await (await loader.getHarness(MatTabGroupHarness)).selectTab({ label: /Equipment/ });
        await vi.waitFor(() => expect(text(element)).toContain("Equipslot0"));
        expect(text(element)).toContain("Equipslot0");
        expect(text(element)).toContain("EquiptypeNotset");
        expect(text(element)).toContain("RenderanimationID123");
        expect(element.textContent).toContain("Gameplay bonuses");
    });

    it("links only explicit relationships including item zero and navigates on the same route", async () => {
        const { element, loader, harness } = await open();
        await (await loader.getHarness(MatTabGroupHarness)).selectTab({ label: /Variants/ });
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
        await vi.waitFor(() => http.expectOne(`${baseUrl}/items/0/definition`).flush({ ...item, id: 0, name: "Linked item" }));
        await harness.fixture.whenStable();
        expect(TestBed.inject(Router).url).toBe("/cache/types/items/0");
        expect(element.textContent).toContain("Linked item");
    });

    it("shows the exact response in the raw tab", async () => {
        const { element, loader } = await open();
        await (await loader.getHarness(MatTabGroupHarness)).selectTab({ label: /Raw data/ });
        await vi.waitFor(() => expect(JSON.parse(element.querySelector("pre")?.textContent || "null")).toEqual(item));
    });

    it("reports item failure, retries and shows loading inside its card", async () => {
        const { element, page, harness } = await open({}, true);
        expect(element.querySelector('[role="alert"]')?.textContent).toContain("Item unavailable");
        page.reload();
        TestBed.tick();
        expect(element.querySelector(".definition-panel mat-progress-bar")).not.toBeNull();
        http.expectOne(`${baseUrl}/items/7/definition`).flush(item);
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
    it("saves the complete definition after confirmation and keeps model data in the payload", async () => {
        const data = { ...item, stackableType: 0, scaleX: 128, maleWornModelId1: 456, extraData: { 451: 123 }, unknownArray1: [1, 2] };
        const { page, harness, element } = await open(data);
        const name = element.querySelector<HTMLInputElement>("#item-name")!;
        name.value = "New blade";
        name.dispatchEvent(new Event("input"));
        const value = element.querySelector<HTMLInputElement>("#item-value")!;
        value.value = "55";
        value.dispatchEvent(new Event("input"));
        TestBed.tick();
        expect(page.dirty()).toBe(true);
        expect(page.canSave()).toBe(true);
        element.querySelector<HTMLButtonElement>(".save-action")!.click();
        TestBed.tick();
        expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(1);
        TestBed.inject(MatDialog).openDialogs[0].close(true);
        const request = await vi.waitFor(() => {
            TestBed.tick();
            return http.expectOne(`${baseUrl}/items/7/definition`);
        });
        expect(request.request.method).toBe("PUT");
        const { stackable, noted, renderAnimationId, ...stored } = data;
        expect(JSON.parse(request.request.serializeBody() as string)).toEqual({ ...stored, name: "New blade", value: 55 });
        expect(request.request.body).not.toHaveProperty("stackable");
        expect(request.request.body).not.toHaveProperty("noted");
        expect(request.request.body).not.toHaveProperty("renderAnimationId");
        request.flush({ operation: "item-updated", type: "item", id: 7 });
        await harness.fixture.whenStable();
        expect(page.dirty()).toBe(false);
        expect(element.textContent).toContain("Changes saved to the game cache");
        expect(element.querySelector("h2")?.textContent).toBe("New blade");
    });

    it("retains drafts after failed saves and rejects invalid names and values", async () => {
        const { page, harness, element } = await open();
        page.updateDraft({ name: "Invalid \u2603" });
        expect(page.canSave()).toBe(false);
        page.updateDraft({ name: "Valid", value: -1 });
        expect(page.canSave()).toBe(false);
        page.updateDraft({ name: "Valid", value: 1 });
        const saving = page.save();
        TestBed.tick();
        TestBed.inject(MatDialog).openDialogs[0].close(true);
        await saving;
        TestBed.tick();
        http.expectOne(`${baseUrl}/items/7/definition`).flush({ detail: "Write failed" }, { status: 503, statusText: "Unavailable" });
        await harness.fixture.whenStable();
        expect(element.textContent).toContain("Write failed");
        expect(element.textContent).toContain("Your draft has been retained");
        expect(page.draft()?.name).toBe("Valid");
        expect(page.canSave()).toBe(true);
    });

    it("duplicates the persisted definition and opens its newly allocated ID", async () => {
        const { page, harness } = await open();
        const duplicating = page.duplicate();
        TestBed.tick();
        TestBed.inject(MatDialog).openDialogs[0].close(true);
        await duplicating;
        TestBed.tick();
        const request = http.expectOne(`${baseUrl}/items/7/duplicate`);
        expect(request.request.method).toBe("POST");
        request.flush({ operation: "item-duplicated", type: "item", id: 8 });
        await vi.waitFor(() => http.expectOne(`${baseUrl}/items/8/definition`).flush({ ...item, id: 8 }));
        await harness.fixture.whenStable();
        expect(TestBed.inject(Router).url).toBe("/cache/types/items/8");
        expect(page.id()).toBe(8);
    });

    it("registers a dirty full-definition draft with the existing navigation guard", async () => {
        const { page } = await open();
        page.updateDraft({ name: "Unsaved" });
        TestBed.tick();
        const leaving = TestBed.inject(CachePendingChangesService).canLeave();
        TestBed.tick();
        TestBed.inject(MatDialog).openDialogs[0].close(false);
        expect(await leaving).toBe(false);
        expect(page.draft()?.name).toBe("Unsaved");
        await page.duplicate();
        http.expectNone(`${baseUrl}/items/7/duplicate`);
    });

    it("forwards preview quantity, outline and shadow and refreshes icons with the loaded definition", async () => {
        const { page, harness, loader } = await open();
        const sprite = previewSprite(harness);
        expect([sprite.quantity(), sprite.outline(), sprite.shadow()]).toEqual([1, 1, "302020"]);
        expect(sprite.revision()).toBe(page.item());
        const portrait = harness.fixture.debugElement.query(By.css(".item-portrait cache-item-sprite-thumbnail")).componentInstance as ItemSpriteBoundary;
        expect(portrait.revision()).toBe(page.item());

        const quantity = await loader.getHarness(MatInputHarness.with({ selector: ".icon-options input[type=number]" }));
        await quantity.setValue("25000");
        await quantity.blur();
        page.setPreviewQuantity("-2");
        page.setPreviewQuantity("1.5");
        await (await loader.getHarness(MatSelectHarness.with({ ancestor: ".icon-options" }))).clickOptions({ text: "Dark and light" });
        await (await loader.getHarness(MatSlideToggleHarness.with({ label: "Shadow" }))).toggle();
        harness.fixture.detectChanges();
        expect([sprite.quantity(), sprite.outline(), sprite.shadow()]).toEqual([25000, 2, "none"]);
    });

    it("zooms the actual item preview and opens the full raw definition from quick actions", async () => {
        const { page, harness, element, loader } = await open();
        const slider = await loader.getHarness(MatSliderHarness);
        await (await slider.getEndThumb()).setValue(1.5);
        expect(page.spriteZoom()).toBe(1.5);
        // Zoom renders the icon at the larger size instead of stretching it.
        expect(previewSprite(harness).size()).toBe(132);
        element.querySelector<HTMLButtonElement>(".quick-panel button")!.click();
        await harness.fixture.whenStable();
        expect(page.selectedTab()).toBe(4);
        await vi.waitFor(() => {
            TestBed.tick();
            expect(JSON.parse(element.querySelector("pre")?.textContent || "null")).toEqual(item);
        });
    });
});
