import { TestBed } from "@angular/core/testing";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { MapPlacement, MapRenderRegion } from "../services/map-render.models";
import { MapScenePanelComponent } from "./map-scene-panel.component";

const crate: MapPlacement = { id: 7, shape: 10, rotation: 0, x: 1, y: 2, plane: 0 };
const region: MapRenderRegion = { id: 12850, regionX: 50, regionY: 50, terrainStatus: "available", landscapeStatus: "available", planes: [], objects: [crate], floors: [], warnings: [] };

describe("MapScenePanelComponent", () => {
    beforeEach(() => TestBed.configureTestingModule({ imports: [MapScenePanelComponent] }));

    async function create(selected?: MapPlacement) {
        const fixture = TestBed.createComponent(MapScenePanelComponent);
        // The panel exists before the region arrives, as it does on the page.
        fixture.componentRef.setInput("regions", []);
        fixture.componentRef.setInput("placements", []);
        fixture.componentRef.setInput("names", new Map());
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.componentRef.setInput("regions", [region]);
        fixture.componentRef.setInput("placements", [{ placement: crate, regionId: region.id }]);
        fixture.componentRef.setInput("names", new Map([[7, "Crate"]]));
        fixture.componentRef.setInput("selected", selected);
        // The tree renders an expansion on its own scheduler, after the effect that requests it.
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(fixture.nativeElement.textContent).toContain("Terrain");
            if (selected) expect(fixture.nativeElement.querySelector(".is-selected")).not.toBeNull();
        });
        return fixture;
    }
    const button = (root: HTMLElement, label: string) => {
        const match = root.querySelector<HTMLButtonElement>(`button[aria-label="${label}"]`);
        if (!match) throw new Error(`Missing button ${label}`);
        return match;
    };
    const text = (fixture: { nativeElement: unknown }) => (fixture.nativeElement as HTMLElement).textContent ?? "";

    it("presents the scene as a tab", async () => {
        const fixture = await create();
        const tabs = (fixture.nativeElement as HTMLElement).querySelectorAll('[role="tab"]');
        expect([...tabs].map(tab => tab.textContent?.trim().replace(/^account_tree\s*/, ""))).toEqual(["Scene"]);
    });

    it("opens the region and lists terrain and its groups", async () => {
        const fixture = await create();
        expect(text(fixture)).toContain("Region 12850");
        expect(text(fixture)).toContain("Terrain");
        expect(text(fixture)).toContain("Objects (1)");
        expect(text(fixture)).not.toContain("Crate");
    });

    it("opens the group of a selection and marks its row", async () => {
        const fixture = await create(crate);
        const row = (fixture.nativeElement as HTMLElement).querySelector(".is-selected");
        expect(row?.textContent).toContain("Crate");
    });

    it("activates a placement and toggles group, terrain and placement visibility", async () => {
        const fixture = await create(crate);
        const root = fixture.nativeElement as HTMLElement;
        const panel = fixture.componentInstance;
        const activated: MapPlacement[] = [];
        panel.placementActivated.subscribe(value => activated.push(value));
        root.querySelector<HTMLButtonElement>(".is-selected .tree-label")?.click();
        expect(activated).toEqual([crate]);

        button(root, "Hide Objects (1)").click();
        expect([...panel.hiddenGroups()]).toEqual(["objects"]);
        await fixture.whenStable();
        expect(button(root, "Show Objects (1)").getAttribute("aria-pressed")).toBe("true");
        button(root, "Show Objects (1)").click();
        expect(panel.hiddenGroups().size).toBe(0);

        button(root, "Hide Terrain").click();
        expect(panel.terrainVisible()).toBe(false);
        await fixture.whenStable();
        button(root, "Hide Crate").click();
        expect([...panel.hiddenPlacements()]).toEqual([crate]);
    });

    it("activates and marks the region row", async () => {
        const fixture = await create();
        const root = fixture.nativeElement as HTMLElement;
        const activated: string[] = [];
        fixture.componentInstance.selectionActivated.subscribe(selection => activated.push(selection));
        const row = [...root.querySelectorAll<HTMLButtonElement>("button.tree-label")].find(label => label.textContent?.includes("Region 12850"));
        expect(row?.disabled).toBe(false);
        row?.click();
        expect(activated).toEqual(["region"]);
        expect(root.querySelector("mat-tree-node.is-selected")).toBeNull();

        fixture.componentRef.setInput("selection", "region");
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(root.querySelector("mat-tree-node.is-selected")?.textContent).toContain("Region 12850");
        });
    });

    it("activates and marks the terrain row", async () => {
        const fixture = await create();
        const root = fixture.nativeElement as HTMLElement;
        const activated: string[] = [];
        fixture.componentInstance.selectionActivated.subscribe(selection => activated.push(selection));
        [...root.querySelectorAll<HTMLButtonElement>("button.tree-label")].find(label => label.textContent?.includes("Terrain"))?.click();
        expect(activated).toEqual(["terrain"]);

        fixture.componentRef.setInput("selection", "terrain");
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(root.querySelector("mat-tree-node.is-selected")?.textContent).toContain("Terrain");
        });
    });

    it("keeps group rows from acting as selections", async () => {
        const root = (await create()).nativeElement as HTMLElement;
        const group = [...root.querySelectorAll<HTMLButtonElement>("button.tree-label")].find(label => label.textContent?.includes("Objects (1)"));
        expect(group?.disabled).toBe(true);
    });

    it("expands matching groups while filtering", async () => {
        const fixture = await create();
        fixture.componentInstance.filter("crate");
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(text(fixture)).toContain("Crate");
        });
    });
});
