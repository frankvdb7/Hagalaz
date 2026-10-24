import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { ApplicationRef, createEnvironmentInjector, EnvironmentInjector } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { environment } from "../../../../environments/environment";
import { CacheArchivesStore } from "./cache-archives.store";
import { CacheConfigDefinitionsStore } from "./cache-config-definitions.store";
import { CacheCs2IntStore } from "./cache-cs2-int.store";
import { cacheDefinitionStores } from "./cache-definition-stores";
import { CacheItemsStore } from "./cache-items.store";
import type { CacheMapsStore } from "./cache-maps.store";
import { CacheNpcsStore } from "./cache-npcs.store";
import { CacheObjectsStore } from "./cache-objects.store";
import { CacheVarpBitsStore } from "./cache-varp-bits.store";

const baseUrl = `${environment.cacheApiUrl}types/`;
const stable = () => TestBed.inject(ApplicationRef).whenStable();
const kinds = Object.keys(cacheDefinitionStores) as (keyof typeof cacheDefinitionStores)[];

describe("independent cache definition stores", () => {
    let http: HttpTestingController;
    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [provideHttpClient(withXhr()), provideHttpClientTesting(), CacheArchivesStore, ...Object.values(cacheDefinitionStores)],
        });
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => http.verify());

    it.each(kinds)("loads only the requested %s definition using signal resources", async (kind) => {
        const store = TestBed.inject(cacheDefinitionStores[kind] as typeof CacheMapsStore);
        expect(store.byIdResult()).toBeNull();
        store.loadById(7);
        TestBed.tick();
        expect(store.loading()).toBe(true);
        http.expectOne(`${baseUrl}${kind}/7`).flush({ id: 7, name: kind });
        await stable();
        expect(store.byIdResult()).toEqual({ id: 7, name: kind });
        expect(store.loading()).toBe(false);
        expect(store.definitionId()).toBe(7);
        expect(store.error()).toBeNull();
    });

    it.each(kinds.filter((kind) => kind !== "maps" && kind !== "cs2-int"))("loads the %s range through its dedicated service", async (kind) => {
        const store = TestBed.inject(cacheDefinitionStores[kind] as typeof CacheItemsStore);
        store.loadRange(10, 25);
        TestBed.tick();
        const request = http.expectOne((r) => r.url === `${baseUrl}${kind}/range`);
        expect(request.request.params.get("startId")).toBe("10");
        expect(request.request.params.get("endIdExclusive")).toBe("25");
        request.flush([{ id: 10, name: kind }]);
        await stable();
        expect(store.rangeResult()).toEqual([{ id: 10, name: kind }]);
    });

    it("keeps item and NPC requests and results independent", async () => {
        const items = TestBed.inject(CacheItemsStore),
            npcs = TestBed.inject(CacheNpcsStore);
        items.loadById(7);
        npcs.loadById(8);
        TestBed.tick();
        expect(items.loading()).toBe(true);
        expect(npcs.loading()).toBe(true);
        http.expectOne(`${baseUrl}items/7`).flush({ id: 7, name: "Sword" });
        http.expectOne(`${baseUrl}npcs/8`).flush({ id: 8, name: "Goblin" });
        await stable();
        expect(items.byIdResult()).toEqual({ id: 7, name: "Sword" });
        expect(npcs.byIdResult()).toEqual({ id: 8, name: "Goblin" });
    });

    it("cancels stale lookups and reloads an unchanged ID", async () => {
        const store = TestBed.inject(CacheItemsStore);
        store.loadById(1);
        TestBed.tick();
        const stale = http.expectOne(`${baseUrl}items/1`);
        store.loadById(2);
        TestBed.tick();
        expect(stale.cancelled).toBe(true);
        http.expectOne(`${baseUrl}items/2`).flush({ id: 2, name: "Current" });
        await stable();
        store.loadById(2);
        TestBed.tick();
        http.expectOne(`${baseUrl}items/2`).flush({ id: 2, name: "Reloaded" });
        await stable();
        expect(store.byIdResult()).toEqual({ id: 2, name: "Reloaded" });
    });

    it("reports read errors without throwing from result signals and retries", async () => {
        const store = TestBed.inject(CacheItemsStore);
        store.loadById(404);
        TestBed.tick();
        http.expectOne(`${baseUrl}items/404`).flush({ detail: "Not found" }, { status: 404, statusText: "Not Found" });
        await stable();
        expect(store.error()).toBe("Not found");
        expect(store.byIdResult()).toBeNull();
        store.loadById(7);
        TestBed.tick();
        http.expectOne(`${baseUrl}items/7`).flush({ id: 7 });
        await stable();
        expect(store.error()).toBeNull();
        expect(store.byIdResult()).toEqual({ id: 7 });
    });

    it("retains zero scalar data and rejects invalid IDs", async () => {
        const store = TestBed.inject(CacheCs2IntStore);
        store.loadById(-1);
        store.loadById(Number.NaN);
        TestBed.tick();
        http.expectNone(() => true);
        store.loadById(0);
        TestBed.tick();
        http.expectOne(`${baseUrl}cs2-int/0`).flush(0);
        await stable();
        expect(store.byIdResult()).toBe(0);
    });

    it("loads range and search resources with real paging and errors", async () => {
        const store = TestBed.inject(CacheItemsStore);
        store.loadRange(20, 25);
        TestBed.tick();
        let request = http.expectOne((req) => req.url === `${baseUrl}items/range`);
        expect(request.request.params.get("startId")).toBe("20");
        expect(request.request.params.get("endIdExclusive")).toBe("25");
        request.flush([]);
        await stable();
        expect(store.rangeResult()).toEqual([]);
        store.search("coin", 10, 25);
        TestBed.tick();
        request = http.expectOne((req) => req.url === `${baseUrl}items/search`);
        expect(request.request.params.get("query")).toBe("coin");
        expect(request.request.params.get("offset")).toBe("10");
        expect(request.request.params.get("limit")).toBe("25");
        request.flush({ query: "coin", totalMatches: 0, offset: 10, limit: 25, items: [] });
        await stable();
        expect(store.searchResult()?.totalMatches).toBe(0);
        store.loadRange(25, 30);
        store.search("fail", 0, 10);
        TestBed.tick();
        http.expectOne((req) => req.url === `${baseUrl}items/range`).flush({ detail: "Range unavailable" }, { status: 503, statusText: "Unavailable" });
        http.expectOne((req) => req.url === `${baseUrl}items/search`).flush({ detail: "Search unavailable" }, { status: 503, statusText: "Unavailable" });
        await stable();
        expect(store.rangeError()).toBe("Range unavailable");
        expect(store.rangeResult()).toBeNull();
        expect(store.searchError()).toBe("Search unavailable");
        expect(store.searchResult()).toBeNull();
    });

    const mutations = [
        {
            kind: "npcs",
            token: CacheNpcsStore,
            body: { combatLevel: 25 },
            path: "npcs/7/combat-level",
            invoke: () => TestBed.inject(CacheNpcsStore).setCombatLevel({ id: 7, combatLevel: 25 }),
        },
        {
            kind: "objects",
            token: CacheObjectsStore,
            body: { name: "Gate" },
            path: "objects/7/name",
            invoke: () => TestBed.inject(CacheObjectsStore).setName({ id: 7, name: "Gate" }),
        },
        {
            kind: "varp-bits",
            token: CacheVarpBitsStore,
            body: { configId: 3, bitLength: 2, bitOffset: 1 },
            path: "varp-bits/7",
            invoke: () => TestBed.inject(CacheVarpBitsStore).setVarpBit({ id: 7, configId: 3, bitLength: 2, bitOffset: 1 }),
        },
        {
            kind: "config-definitions",
            token: CacheConfigDefinitionsStore,
            body: { defaultValue: 0, valueType: "i" },
            path: "config-definitions/7",
            invoke: () => TestBed.inject(CacheConfigDefinitionsStore).setConfigDefinition({ id: 7, defaultValue: 0, valueType: "i" }),
        },
    ];
    it.each(mutations)("confirms $kind writes and updates only its loaded definition", async ({ kind, token, path, body, invoke }) => {
        const store = TestBed.inject(token as typeof CacheMapsStore);
        store.loadById(7);
        TestBed.tick();
        invoke();
        http.expectNone((req) => req.method === "POST");
        http.expectOne(`${baseUrl}${kind}/7`).flush({ id: 7, name: "Before" });
        await stable();
        invoke();
        invoke();
        const request = http.expectOne(`${baseUrl}${path}`);
        expect(store.loading()).toBe(true);
        expect(request.request.method).toBe("POST");
        expect(request.request.body).toEqual(body);
        request.flush({ operation: "update", type: kind, id: 7 });
        expect(store.mutationResult()?.id).toBe(7);
        expect(store.loading()).toBe(false);
        expect(store.byIdResult()).toEqual({ id: 7, name: "Before", ...body });
        invoke();
        http.expectOne(`${baseUrl}${path}`).flush({ detail: "Read only" }, { status: 409, statusText: "Conflict" });
        expect(store.error()).toBe("Read only");
        expect(store.loading()).toBe(false);
        expect(store.mutationResult()).toBeNull();
    });

    it("does not apply a save response to a different route ID", async () => {
        const store = TestBed.inject(CacheNpcsStore);
        store.loadById(7);
        TestBed.tick();
        http.expectOne(`${baseUrl}npcs/7`).flush({ id: 7, combatLevel: 2 });
        await stable();
        store.setCombatLevel({ id: 7, combatLevel: 42 });
        const save = http.expectOne(`${baseUrl}npcs/7/combat-level`);
        store.loadById(8);
        TestBed.tick();
        save.flush({ operation: "updated", type: "npcs", id: 7 });
        http.expectOne(`${baseUrl}npcs/8`).flush({ id: 8, combatLevel: 3 });
        await stable();
        expect(store.byIdResult()).toEqual({ id: 8, combatLevel: 3 });
        expect(store.definitionId()).toBe(8);
    });

    it("destroys pending resources with their page injector", () => {
        const child = createEnvironmentInjector([CacheItemsStore], TestBed.inject(EnvironmentInjector));
        const store = child.get(CacheItemsStore);
        store.loadById(7);
        TestBed.tick();
        const request = http.expectOne(`${baseUrl}items/7`);
        child.destroy();
        expect(request.cancelled).toBe(true);
    });

    it("loads and retries archive resources on demand", async () => {
        const store = TestBed.inject(CacheArchivesStore);
        TestBed.tick();
        http.expectNone(() => true);
        store.loadArchiveSizes();
        TestBed.tick();
        expect(store.loading()).toBe(true);
        http.expectOne(`${baseUrl}archive-sizes`).flush({ detail: "Unavailable" }, { status: 503, statusText: "Unavailable" });
        await stable();
        expect(store.error()).toBe("Unavailable");
        expect(store.archiveSizes()).toBeNull();
        store.loadArchiveSizes();
        TestBed.tick();
        http.expectOne(`${baseUrl}archive-sizes`).flush({ items: 0 });
        await stable();
        expect(store.archiveSizes()).toEqual({ items: 0 });
        expect(store.error()).toBeNull();
    });
});
