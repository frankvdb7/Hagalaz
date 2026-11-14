import { TestbedHarnessEnvironment } from "@angular/cdk/testing/testbed";
import { Component, DestroyRef, inject, signal } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { MatDialog } from "@angular/material/dialog";
import { MatTabNavBarHarness } from "@angular/material/tabs/testing";
import { By } from "@angular/platform-browser";
import { provideRouter, Router } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CachePendingChangesService, cacheCanActivateChild } from "../cache-pending-changes.service";
import { CacheShellPageComponent } from "./cache-shell.page";

@Component({ template: "<p>Tool content</p>" })
class ToolContent {
    readonly dirty = signal(false);
    readonly saving = signal(false);

    constructor() {
        inject(CachePendingChangesService).register(this, inject(DestroyRef));
    }
}

describe("Cache Studio resource navigation", () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                provideRouter([
                    {
                        path: "cache",
                        component: CacheShellPageComponent,
                        providers: [CachePendingChangesService],
                        canActivateChild: [cacheCanActivateChild],
                        children: [
                            { path: "types", component: ToolContent },
                            { path: "types/:kind/:id", component: ToolContent },
                            { path: "models", component: ToolContent },
                            { path: "widgets", component: ToolContent },
                            { path: "widgets/:id", component: ToolContent },
                        ],
                    },
                ]),
            ],
        });
    });

    afterEach(() => TestBed.resetTestingModule());

    async function open(url = "/cache/types") {
        const harness = await RouterTestingHarness.create(url);
        const nav = await TestbedHarnessEnvironment.loader(harness.fixture).getHarness(MatTabNavBarHarness);
        return { harness, nav, router: TestBed.inject(Router) };
    }

    async function selectedLabel(nav: MatTabNavBarHarness) {
        return (await (await nav.getActiveLink()).host()).getAttribute("aria-label");
    }

    it.each([
        ["/cache/types?kind=npcs&query=guard", "NPCs"],
        ["/cache/types/items/7", "Items"],
        ["/cache/widgets/12", "Widgets"],
    ])("selects the matching cache type for %s", async (url, label) => {
        const { nav } = await open(url);
        expect(await selectedLabel(nav)).toBe(label);
        expect(await (await nav.getPanel()).getTextContent()).toBe("Tool content");
    });

    it.each(["/cache/types?kind=sprites&offset=10", "/cache/types/sprites/7"])("selects only Sprites for %s", async (url) => {
        const { nav } = await open(url);
        expect(await selectedLabel(nav)).toBe("Sprites");
        const selected = await Promise.all((await nav.getLinks()).map((link) => link.isActive()));
        expect(selected.filter(Boolean)).toHaveLength(1);
    });

    it("navigates between cache types and dedicated browsers", async () => {
        const { nav, router, harness } = await open();
        expect(await Promise.all((await nav.getLinks()).map(async (link) => (await link.host()).getAttribute("aria-label")))).toEqual([
            "Items",
            "NPCs",
            "Objects",
            "Animations",
            "Maps",
            "Sprites",
            "Quests",
            "Graphics",
            "Varp bits",
            "Client map definitions",
            "Config definitions",
            "CS2 scripts",
            "CS2 integers",
            "Models",
            "Widgets",
        ]);
        for (const [label, path] of [
            ["Models", "models"],
            ["Widgets", "widgets"],
            ["Sprites", "types?kind=sprites"],
            ["Items", "types?kind=items"],
        ]) {
            await nav.clickLink({ label: new RegExp(`${label}$`) });
            expect(router.url).toBe(`/cache/${path}`);
            expect(await selectedLabel(nav)).toBe(label);
        }
        expect(harness.routeNativeElement?.textContent).not.toContain("Browse game definitions and edit supported fields.");
    });

    it("retains the current resource when the existing dirty-draft guard cancels navigation", async () => {
        const { nav, router, harness } = await open("/cache/types/items/7");
        const editor = harness.fixture.debugElement.query(By.directive(ToolContent)).componentInstance as ToolContent;
        editor.dirty.set(true);
        const navigation = nav.clickLink({ label: /Models$/ });
        const dialog = TestBed.inject(MatDialog);
        await vi.waitFor(() => expect(dialog.openDialogs).toHaveLength(1));
        dialog.openDialogs[0].close(false);
        await navigation;
        expect(router.url).toBe("/cache/types/items/7");
        expect(await selectedLabel(nav)).toBe("Items");
        expect(editor.dirty()).toBe(true);
    });
});
