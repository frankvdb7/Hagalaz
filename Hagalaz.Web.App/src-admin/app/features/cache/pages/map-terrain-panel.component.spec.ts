import { TestBed } from "@angular/core/testing";
import { EMPTY } from "rxjs";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CacheMapsService } from "../services/cache-maps.service";
import type { MapFloor } from "../services/map-render.models";
import { MapFloorPickerComponent } from "./map-floor-picker.component";
import type { TerrainBrush } from "./map-terrain-edit";
import { MapTerrainPanelComponent } from "./map-terrain-panel.component";

const brush: TerrainBrush = { tool: "underlay", plane: 0, size: 3, strength: 2, underlay: null, overlay: null, overlayShape: 0, overlayRotation: 0 };
const floor = (id: number, overlay: boolean, textureId = -1): MapFloor => ({ id, overlay, rgb: 0x102030, textureId, secondaryRgb: null, textureScale: 512 });
const text = (fixture: { nativeElement: unknown }) => (fixture.nativeElement as HTMLElement).textContent ?? "";

function provideFloorImages(): void {
    vi.stubGlobal("IntersectionObserver", class {
        observe(): void { /* Rows stay out of view, so no image is requested. */ }
        disconnect(): void { /* Nothing to release. */ }
    });
    TestBed.overrideProvider(CacheMapsService, { useValue: { texture: () => EMPTY } });
}

afterEach(() => vi.unstubAllGlobals());

describe("MapTerrainPanelComponent", () => {
    beforeEach(() => {
        TestBed.configureTestingModule({ imports: [MapTerrainPanelComponent] });
        provideFloorImages();
    });

    function create(start: Partial<TerrainBrush> = {}) {
        const fixture = TestBed.createComponent(MapTerrainPanelComponent);
        fixture.componentRef.setInput("brush", { ...brush, ...start });
        fixture.componentRef.setInput("floors", [floor(12, false, 5), floor(3, true)]);
        fixture.detectChanges();
        return fixture;
    }

    it("presents the panel as a tab", () => {
        const tab = (create().nativeElement as HTMLElement).querySelector('[role="tab"]');
        expect(tab?.textContent).toContain("Terrain");
    });

    it("marks the active tool and switches tools", async () => {
        const fixture = create({ tool: "raise" });
        const root = fixture.nativeElement as HTMLElement;
        const tool = (label: string) => root.querySelector<HTMLButtonElement>(`button[aria-label="${label}"]`);
        expect(tool("Raise")?.getAttribute("aria-pressed")).toBe("true");
        tool("Flatten")?.click();
        await fixture.whenStable();
        expect(fixture.componentInstance.brush().tool).toBe("flatten");
        expect(tool("Flatten")?.getAttribute("aria-pressed")).toBe("true");
        expect(tool("Raise")?.getAttribute("aria-pressed")).toBe("false");
    });

    it("shows strength only for raise and lower, and no floors", () => {
        const fixture = create({ tool: "raise" });
        expect(text(fixture)).toContain("Strength");
        expect(text(fixture)).not.toContain("Find a floor");
        expect(text(fixture)).not.toContain("Shape");
    });

    it("shows shape, rotation and overlays for the overlay brush and chooses one", () => {
        const fixture = create({ tool: "overlay" });
        expect(text(fixture)).toContain("Shape");
        expect(text(fixture)).toContain("Rotation");
        expect(text(fixture)).toContain("Overlay 3");
        expect(text(fixture)).not.toContain("Underlay 12");
        fixture.componentInstance.chooseFloor(3);
        expect(fixture.componentInstance.brush().overlay).toBe(3);
        expect(fixture.componentInstance.brush().underlay).toBeNull();
    });

    it("chooses an underlay for the underlay brush", () => {
        const panel = create().componentInstance;
        panel.chooseFloor(12);
        expect(panel.brush().underlay).toBe(12);
    });

    it("updates one brush field at a time", () => {
        const panel = create().componentInstance;
        panel.update({ size: 7 });
        expect(panel.brush()).toEqual({ ...brush, size: 7 });
    });
});

describe("MapFloorPickerComponent", () => {
    beforeEach(() => {
        TestBed.configureTestingModule({ imports: [MapFloorPickerComponent] });
        provideFloorImages();
    });

    function create(floors: MapFloor[] = [floor(12, false, 5), floor(4, false), floor(3, true)]) {
        const fixture = TestBed.createComponent(MapFloorPickerComponent);
        fixture.componentRef.setInput("floors", floors);
        fixture.detectChanges();
        return fixture;
    }

    it("lists one kind of floor with its colour and texture", () => {
        const fixture = create();
        expect(text(fixture)).toContain("Underlay 12");
        expect(text(fixture)).toContain("Texture 5");
        expect(text(fixture)).not.toContain("Overlay 3");
        expect((fixture.nativeElement as HTMLElement).querySelectorAll("cache-map-floor-swatch")).toHaveLength(2);
    });

    it("filters by ID and says when nothing matches", async () => {
        const fixture = create();
        fixture.componentInstance.query.set("12");
        await fixture.whenStable();
        expect(fixture.componentInstance.visibleFloors().map(item => item.id)).toEqual([12]);
        fixture.componentInstance.query.set("99");
        await fixture.whenStable();
        expect(text(fixture)).toContain("No matching floors.");
    });

    it("reports a chosen floor when clicked", () => {
        const fixture = create();
        const chosen: number[] = [];
        fixture.componentInstance.chosenChange.subscribe(id => chosen.push(id));
        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>("button[mat-list-item]")?.click();
        expect(chosen).toEqual([12]);
    });

    it("shows loading and error states", async () => {
        const fixture = create([]);
        fixture.componentRef.setInput("loading", true);
        await fixture.whenStable();
        expect(text(fixture)).toContain("Loading floors");
        fixture.componentRef.setInput("error", "Floors are unavailable.");
        await fixture.whenStable();
        expect(text(fixture)).toContain("Floors are unavailable.");
    });
});
