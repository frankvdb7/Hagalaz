import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { ApplicationRef } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { environment } from "../../../../environments/environment";
import { CacheMapRegionsStore } from "./cache-map-regions.store";

const url = `${environment.cacheApiUrl}types/maps`;
const region = { id: 258, regionX: 1, regionY: 2, terrainFileId: 17, landscapeFileId: null };
const page = (offset = 0) => ({ items: [region], total: 23, offset, limit: 10 });

describe("CacheMapRegionsStore region listing", () => {
    let http: HttpTestingController;
    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [CacheMapRegionsStore, provideHttpClient(withXhr()), provideHttpClientTesting()] });
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => http.verify());
    const stable = () => TestBed.inject(ApplicationRef).whenStable();

    it("loads metadata pages and keeps the displayed page steady during replacement", async () => {
        const store = TestBed.inject(CacheMapRegionsStore);
        http.expectNone(() => true);
        store.loadPage(0, 10);
        TestBed.tick();
        expect(store.rangeLoading()).toBe(true);
        http.expectOne(`${url}?offset=0&limit=10`).flush(page());
        await stable();
        expect(store.rangeResult()).toEqual([region]);
        expect(store.regionPage()?.total).toBe(23);
        store.loadPage(10, 10);
        TestBed.tick();
        expect(store.rangePagination()).toEqual({ pageIndex: 0, pageSize: 10 });
        expect(store.rangeResult()).toEqual([region]);
        http.expectOne(`${url}?offset=10&limit=10`).flush(page(10));
        await stable();
        expect(store.rangePagination()).toEqual({ pageIndex: 1, pageSize: 10 });
        expect(store.rangeLoading()).toBe(false);
    });

    it("reports unavailable metadata, clears stale rows and retries the same page", async () => {
        const store = TestBed.inject(CacheMapRegionsStore);
        store.loadPage(0, 10);
        TestBed.tick();
        http.expectOne(`${url}?offset=0&limit=10`).flush(page());
        await stable();
        store.loadPage(0, 10);
        TestBed.tick();
        http.expectOne(`${url}?offset=0&limit=10`).flush({ detail: "Map index has no named metadata" }, { status: 422, statusText: "Unprocessable Entity" });
        await stable();
        expect(store.rangeError()).toBe("Map index has no named metadata");
        expect(store.rangeResult()).toBeNull();
        expect(store.rangeLoading()).toBe(false);
        store.loadPage(0, 10);
        TestBed.tick();
        http.expectOne(`${url}?offset=0&limit=10`).flush({ items: [], total: 0, offset: 0, limit: 10 });
        await stable();
        expect(store.rangeError()).toBeNull();
        expect(store.rangeResult()).toEqual([]);
    });

    it("rejects invalid pages and prevents duplicate pending requests", async () => {
        const store = TestBed.inject(CacheMapRegionsStore);
        for (const [offset, limit] of [[-1, 10], [65537, 10], [0, 0], [0, 201], [0.5, 10], [0, 1.5]]) store.loadPage(offset, limit);
        TestBed.tick();
        http.expectNone(() => true);
        store.loadPage(0, 10);
        TestBed.tick();
        const request = http.expectOne(`${url}?offset=0&limit=10`);
        store.loadPage(0, 10);
        TestBed.tick();
        http.expectNone(() => true);
        request.flush(page());
        await stable();
        expect(store.rangeResult()).toEqual([region]);
    });

    it("cancels obsolete pages and releases requests on destruction", async () => {
        const store = TestBed.inject(CacheMapRegionsStore);
        store.loadPage(0, 10);
        TestBed.tick();
        const first = http.expectOne(`${url}?offset=0&limit=10`);
        store.loadPage(10, 10);
        TestBed.tick();
        expect(first.cancelled).toBe(true);
        const next = http.expectOne(`${url}?offset=10&limit=10`);
        TestBed.resetTestingModule();
        expect(next.cancelled).toBe(true);
    });
});
