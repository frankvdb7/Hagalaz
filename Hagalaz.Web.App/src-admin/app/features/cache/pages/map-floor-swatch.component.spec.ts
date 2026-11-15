import { TestBed } from "@angular/core/testing";
import { of, Subject, throwError } from "rxjs";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CacheMapsService } from "../services/cache-maps.service";
import type { MapFloor } from "../services/map-render.models";
import { MapFloorSwatchComponent } from "./map-floor-swatch.component";

const floor = (rgb: number | null, textureId: number): MapFloor => ({ id: 1, overlay: false, rgb, textureId, secondaryRgb: null, textureScale: 512 });
const loaded = { blob: new Blob(), material: {} };

describe("MapFloorSwatchComponent", () => {
    let reveal: () => void;
    const texture = vi.fn();

    beforeEach(() => {
        texture.mockReset();
        vi.stubGlobal("IntersectionObserver", class {
            constructor(callback: (entries: { isIntersecting: boolean }[]) => void) { reveal = () => callback([{ isIntersecting: true }]); }
            observe(): void { /* The test decides when the row is visible. */ }
            disconnect(): void { /* Nothing to release. */ }
        });
        vi.stubGlobal("URL", class extends URL {
            static override createObjectURL = vi.fn(() => "blob:floor");
            static override revokeObjectURL = vi.fn();
        });
        TestBed.configureTestingModule({ imports: [MapFloorSwatchComponent], providers: [{ provide: CacheMapsService, useValue: { texture } }] });
    });
    afterEach(() => vi.unstubAllGlobals());

    async function create(value: MapFloor) {
        const fixture = TestBed.createComponent(MapFloorSwatchComponent);
        fixture.componentRef.setInput("floor", value);
        await fixture.whenStable();
        return fixture;
    }

    it("shows a solid floor's colour without requesting an image", async () => {
        const fixture = await create(floor(0x102030, -1));
        reveal();
        await fixture.whenStable();
        expect(texture).not.toHaveBeenCalled();
        expect(fixture.componentInstance.color()).toBe("#102030");
    });

    it("waits until the row is near the viewport to load a texture", async () => {
        texture.mockReturnValue(of(loaded));
        const fixture = await create(floor(0x102030, 7));
        expect(texture).not.toHaveBeenCalled();
        reveal();
        await fixture.whenStable();
        expect(texture).toHaveBeenCalledExactlyOnceWith(7);
        expect((fixture.nativeElement as HTMLElement).querySelector("img")?.getAttribute("src")).toBe("blob:floor");
    });

    it("has no colour to show for a texture-only floor", async () => {
        expect((await create(floor(0, 7))).componentInstance.color()).toBe("transparent");
    });

    it("keeps the colour when a texture cannot be loaded", async () => {
        texture.mockReturnValue(throwError(() => new Error("Missing")));
        const fixture = await create(floor(0x102030, 7));
        reveal();
        await fixture.whenStable();
        expect((fixture.nativeElement as HTMLElement).querySelector("img")).toBeNull();
        expect(fixture.componentInstance.color()).toBe("#102030");
    });

    it("releases the image and cancels a pending request when destroyed", async () => {
        texture.mockReturnValue(of(loaded));
        const fixture = await create(floor(1, 7));
        reveal();
        await fixture.whenStable();
        fixture.destroy();
        expect(URL.revokeObjectURL).toHaveBeenCalledExactlyOnceWith("blob:floor");

        const pending = new Subject<typeof loaded>();
        texture.mockReturnValue(pending);
        const second = await create(floor(1, 8));
        reveal();
        await second.whenStable();
        second.destroy();
        expect(pending.observed).toBe(false);
    });
});
