import { signal, type Signal } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { CacheObjectsService } from "../services/cache-objects.service";
import { MapAssetPanelComponent } from "./map-asset-panel.component";
import { mapObjectDragType } from "./map-viewport.component";

describe("MapAssetPanelComponent", () => {
    let query: Signal<Record<string, unknown> | null>;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [MapAssetPanelComponent],
            providers: [{ provide: CacheObjectsService, useValue: { catalogResource: (page: typeof query) => { query = page; return { hasValue: () => false, isLoading: signal(false), error: signal(undefined) }; } } }],
        });
    });

    function create(active = true) {
        const fixture = TestBed.createComponent(MapAssetPanelComponent);
        fixture.componentRef.setInput("active", active);
        return fixture.componentInstance;
    }

    it("waits for a loaded region before requesting the catalog", () => {
        create(false);
        expect(query()).toBeNull();
    });

    it("requests every type by default", () => {
        create();
        expect(query()).toEqual({ query: "", offset: 0, limit: 40 });
    });

    it("filters by the chosen type's shapes and returns to the first page", () => {
        const panel = create();
        panel.catalogOffset.set(80);
        panel.changeType("walls");
        expect(query()).toEqual({ query: "", offset: 0, limit: 40, shapes: "0,1,2,3,9" });
        panel.changeType("");
        expect(query()).toEqual({ query: "", offset: 0, limit: 40 });
    });

    describe("dragging an object", () => {
        const dragEvent = () => {
            const dataTransfer = { setData: vi.fn(), setDragImage: vi.fn(), effectAllowed: "" };
            return { dataTransfer, event: { dataTransfer, currentTarget: document.createElement("button") } as unknown as DragEvent };
        };

        it("drags a compact preview of the object instead of its row", () => {
            const fixture = TestBed.createComponent(MapAssetPanelComponent);
            fixture.detectChanges();
            const panel = fixture.componentInstance;
            const { dataTransfer, event } = dragEvent();
            const fence = { id: 99, name: "Fence", shape: 10 };

            panel.dragObject(event, fence);

            const preview = fixture.nativeElement.querySelector(".drag-preview");
            expect(dataTransfer.setDragImage).toHaveBeenCalledWith(preview, 28, 28);
            expect(preview.textContent).toContain("Fence");
            expect(preview.querySelector("canvas").hidden).toBe(true);
            expect(dataTransfer.setData).toHaveBeenCalledWith(mapObjectDragType, "99");
            expect(dataTransfer.effectAllowed).toBe("copy");
            expect(panel.selectedAsset()).toEqual(fence);
            expect(panel.draggingId()).toBe(99);
        });

        it("starts no drag for an object without a placeable model", () => {
            const fixture = TestBed.createComponent(MapAssetPanelComponent);
            fixture.detectChanges();
            const { dataTransfer, event } = dragEvent();

            fixture.componentInstance.dragObject(event, { id: 5, name: "Nothing", shape: null });

            expect(dataTransfer.setData).not.toHaveBeenCalled();
            expect(fixture.componentInstance.draggingId()).toBeNull();
        });
    });
});
