import { signal, type Signal } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { beforeEach, describe, expect, it } from "vitest";
import { CacheObjectsService } from "../services/cache-objects.service";
import { MapAssetPanelComponent } from "./map-asset-panel.component";

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
});
