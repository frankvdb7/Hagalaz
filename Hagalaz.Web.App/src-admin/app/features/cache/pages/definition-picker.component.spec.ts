import { InteractivityChecker } from "@angular/cdk/a11y";
import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { provideRouter, Router } from "@angular/router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../environments/environment";
import { cacheTypeNames, type TypeKind } from "../services/cache.models";
import { cacheDefinitionStores } from "../stores/cache-definition-stores";
import { DefinitionPickerComponent, type DefinitionPickerStore } from "./definition-picker.component";

const url = `${environment.cacheApiUrl}types/items/search`;

describe("cache definition breadcrumb picker", () => {
    let http: HttpTestingController;
    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                ...Object.values(cacheDefinitionStores),
                provideHttpClient(withXhr()),
                provideHttpClientTesting(),
                provideRouter(Object.keys(cacheTypeNames).map((kind) => ({ path: `cache/types/${kind}/:id`, children: [] }))),
            ],
        });
        http = TestBed.inject(HttpTestingController);
        // jsdom has no layout boxes; keep CDK's focus tests tied to real controls.
        vi.spyOn(TestBed.inject(InteractivityChecker), "isFocusable").mockImplementation((element) => element.matches("input,button"));
        vi.spyOn(TestBed.inject(InteractivityChecker), "isTabbable").mockImplementation((element) => element.matches("input,button"));
    });
    afterEach(() => {
        TestBed.resetTestingModule();
        http.verify({ ignoreCancelled: true });
        vi.restoreAllMocks();
    });

    async function open(kind: TypeKind = "items") {
        const fixture = TestBed.createComponent(DefinitionPickerComponent);
        const store = TestBed.inject<DefinitionPickerStore & { loadById(id: number): void }>(cacheDefinitionStores[kind]);
        fixture.componentRef.setInput("store", store);
        fixture.componentRef.setInput("kind", kind);
        store.loadById(7);
        TestBed.tick();
        http.expectOne(`${environment.cacheApiUrl}types/${kind}/7`).flush(
            kind === "cs2-int" ? 0 : { id: 7, ...(kind === "items" ? { name: "Runic blade" } : {}) }
        );
        fixture.componentRef.setInput("id", 7);
        await fixture.whenStable();
        const trigger = fixture.nativeElement.querySelector("button") as HTMLButtonElement;
        trigger.click();
        await fixture.whenStable();
        const input = document.querySelector<HTMLInputElement>('input[type="search"]');
        if (!input) throw new Error("Expected item autocomplete input");
        const type = (value: string) => {
            input.value = value;
            input.dispatchEvent(new Event("input", { bubbles: true }));
            TestBed.tick();
        };
        return { fixture, trigger, input, type, picker: fixture.componentInstance };
    }

    it("opens from the item name without fetching the archive and restores focus on close", async () => {
        const { fixture, trigger, input, picker } = await open();
        expect(trigger.textContent).toContain("Runic blade #7");
        expect(document.activeElement).toBe(input);
        http.expectNone(() => true);
        expect(document.querySelector('[role="dialog"]')).not.toBeNull();
        picker.close();
        await fixture.whenStable();
        expect(document.querySelector('[role="dialog"]')).toBeNull();
        expect(document.activeElement).toBe(trigger);
    });

    it("debounces names, shows actual suggestions, and navigates to the selected item", async () => {
        const { fixture, type, picker } = await open();
        type("bl");
        type("blade");
        expect(document.querySelector("mat-progress-bar")).not.toBeNull();
        await vi.waitFor(() =>
            http
                .expectOne((request) => request.url === url && request.params.get("query") === "blade" && request.params.get("limit") === "10")
                .flush({ query: "blade", totalMatches: 1, items: [{ id: 42, name: "Steel blade" }] })
        );
        await fixture.whenStable();
        expect(document.querySelector('[role="listbox"]')?.textContent).toContain("Steel blade");
        const option = document.querySelector<HTMLElement>("mat-option:not([aria-disabled=true])");
        if (!option) throw new Error("Expected matching item");
        option.click();
        await fixture.whenStable();
        expect(TestBed.inject(Router).url).toBe("/cache/types/items/42");
        expect(picker.opened()).toBe(false);
        http.expectNone((request) => request.params.get("query") === "bl");
    });

    it("allows item zero without a name search and rejects unsafe IDs", async () => {
        const { fixture, type, picker } = await open();
        type("0");
        await fixture.whenStable();
        expect(picker.directId()).toBe(0);
        http.expectNone(() => true);
        expect(document.querySelector('[role="listbox"]')?.textContent).toContain("Open Item #0");
        picker.choose(0);
        await fixture.whenStable();
        expect(TestBed.inject(Router).url).toBe("/cache/types/items/0");
        picker.open();
        TestBed.tick();
        picker.searchForm.query().value.set("9007199254740992");
        TestBed.tick();
        expect(picker.directId()).toBeNull();
        picker.close();
        await fixture.whenStable();
        http.expectNone(() => true);
    });

    it("shows errors and empty results, and retries the current query", async () => {
        const { fixture, type, picker } = await open();
        type("blade");
        await vi.waitFor(() =>
            http.expectOne((request) => request.url === url).flush({ detail: "Search offline" }, { status: 503, statusText: "Unavailable" })
        );
        await fixture.whenStable();
        expect(document.querySelector('[role="alert"]')?.textContent).toContain("Search offline");
        picker.retry();
        TestBed.tick();
        http.expectOne((request) => request.url === url).flush({ query: "blade", totalMatches: 0, items: [] });
        await fixture.whenStable();
        expect(document.querySelector('[role="listbox"]')?.textContent).toContain("No matching items");
        expect(picker.error()).toBeNull();
    });

    it("cancels the debounce when closed and closes on Escape or outside click", async () => {
        const { fixture, type, input, trigger, picker } = await open();
        type("blade");
        input.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape", bubbles: true }));
        await fixture.whenStable();
        expect(picker.opened()).toBe(false);
        expect(document.activeElement).toBe(trigger);
        http.expectNone(() => true);
        picker.open();
        await fixture.whenStable();
        document.querySelector<HTMLElement>(".cdk-overlay-backdrop")?.click();
        await fixture.whenStable();
        expect(picker.opened()).toBe(false);
    });

    it.each(Object.keys(cacheTypeNames) as TypeKind[])("navigates to ID zero within %s", async (kind) => {
        const { fixture, type, picker, trigger } = await open(kind);
        expect(trigger.getAttribute("aria-label")).toContain(`Choose ${cacheTypeNames[kind]}.`);
        type("0");
        await fixture.whenStable();
        expect(document.querySelector('[role="listbox"]')?.textContent).toContain(`Open ${cacheTypeNames[kind]} #0`);
        picker.choose("0");
        await fixture.whenStable();
        expect(TestBed.inject(Router).url).toBe(`/cache/types/${kind}/0`);
        http.expectNone(() => true);
    });

    it("uses NPC name search through its existing store", async () => {
        const { fixture, type, picker } = await open("npcs");
        type("goblin");
        await vi.waitFor(() =>
            http
                .expectOne((request) => request.url.endsWith("/npcs/search") && request.params.get("query") === "goblin")
                .flush({ query: "goblin", totalMatches: 1, items: [{ id: 8, name: "Goblin" }] })
        );
        await fixture.whenStable();
        expect(document.querySelector('[role="listbox"]')?.textContent).toContain("Goblin");
        picker.choose("8");
        await fixture.whenStable();
        expect(TestBed.inject(Router).url).toBe("/cache/types/npcs/8");
    });

    it.each((Object.keys(cacheTypeNames) as TypeKind[]).filter((kind) => kind !== "items" && kind !== "npcs"))(
        "does not invent name searches for %s",
        async (kind) => {
            const { fixture, type, picker } = await open(kind);
            type("dragon");
            await fixture.whenStable();
            expect(picker.canSearch()).toBe(false);
            expect(picker.pending()).toBe(false);
            expect(document.querySelector('[role="listbox"]')?.textContent).toContain("Enter a nonnegative whole-number ID");
            http.expectNone(() => true);
        }
    );
});
