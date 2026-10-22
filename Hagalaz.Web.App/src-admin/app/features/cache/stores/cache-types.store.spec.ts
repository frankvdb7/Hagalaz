import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { createEnvironmentInjector, EnvironmentInjector } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { environment } from "../../../../environments/environment";
import type { ArchiveSizesDto, SearchResultDto, TypeMutationResultDto } from "../services/cache.models";
import { CacheTypesStore } from "./cache-types.store";

type TypesStore = InstanceType<typeof CacheTypesStore>;
const baseUrl = `${environment.cacheApiUrl}types/`;

describe("CacheTypesStore", () => {
    let store: TypesStore;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [provideHttpClient(withXhr()), provideHttpClientTesting(), CacheTypesStore],
        });
        store = TestBed.inject(CacheTypesStore);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it("starts without results, errors, or a pending request", () => {
        expect({
            archives: store.archiveSizes(),
            definition: store.byIdResult(),
            range: store.rangeResult(),
            search: store.searchResult(),
            mutation: store.mutationResult(),
            error: store.error(),
            loading: store.loading(),
        }).toEqual({ archives: null, definition: null, range: null, search: null, mutation: null, error: null, loading: false });
        http.expectNone(() => true);
    });

    it("loads archive counts and clears loading after the response", async () => {
        const result: ArchiveSizesDto = {
            items: 100,
            npcs: 20,
            objects: 30,
            sprites: 40,
            quests: 5,
            animations: 6,
            graphics: 7,
            cs2: 8,
            clientMapDefinitions: 9,
            configDefinitions: 10,
            varpBits: 11,
            maps: 12,
        };
        const pending = store.loadArchiveSizes();
        expect(store.loading()).toBe(true);
        expect(store.archiveSizes()).toBeNull();

        const request = http.expectOne(`${baseUrl}archive-sizes`);
        expect(request.request.method).toBe("GET");
        request.flush(result);
        await pending;

        expect(store.archiveSizes()).toEqual(result);
        expect(store.loading()).toBe(false);
        expect(store.error()).toBeNull();
    });

    it("loads a definition for the requested type and ID", async () => {
        const result = { id: 42, name: "Bronze sword" };
        const pending = store.loadById("items", 42);
        expect(store.loading()).toBe(true);
        const request = http.expectOne(`${baseUrl}items/42`);
        expect(request.request.method).toBe("GET");
        request.flush(result);
        await pending;
        expect(store.byIdResult()).toEqual(result);
        expect(store.loading()).toBe(false);
    });

    it("preserves a zero-valued definition", async () => {
        const pending = store.loadById("cs2-int", 0);
        http.expectOne(`${baseUrl}cs2-int/0`).flush(0);
        await pending;
        expect(store.byIdResult()).toBe(0);
        expect(store.error()).toBeNull();
    });

    it("loads an empty ID range with exclusive end bounds", async () => {
        const pending = store.loadRange("objects", 0, 10);
        const request = http.expectOne((req) => req.url === `${baseUrl}objects/range`);
        expect(request.request.method).toBe("GET");
        expect(request.request.params.get("startId")).toBe("0");
        expect(request.request.params.get("endIdExclusive")).toBe("10");
        request.flush([]);
        await pending;
        expect(store.rangeResult()).toEqual([]);
        expect(store.loading()).toBe(false);
        expect(store.error()).toBeNull();
    });

    it.each([
        { name: "matching", items: [{ id: 42, name: "Bronze sword" }], totalMatches: 12 },
        { name: "empty", items: [], totalMatches: 0 },
    ])("publishes $name search results with the requested paging", async ({ items, totalMatches }) => {
        const result: SearchResultDto<unknown> = { query: "bronze sword", totalMatches, offset: 5, limit: 20, items };
        const pending = store.search("items", result.query, 5, 20);
        const request = http.expectOne((req) => req.url === `${baseUrl}items/search`);
        expect(request.request.params.get("query")).toBe("bronze sword");
        expect(request.request.params.get("offset")).toBe("5");
        expect(request.request.params.get("limit")).toBe("20");
        request.flush(result);
        await pending;
        expect(store.searchResult()).toEqual(result);
        expect(store.loading()).toBe(false);
        expect(store.error()).toBeNull();
    });

    it.each([
        {
            name: "NPC combat level",
            path: "npcs/42/combat-level",
            type: "npcs",
            body: { combatLevel: 25 },
            invoke: (sut: TypesStore) => sut.setNpcCombatLevel(42, 25),
        },
        {
            name: "object name",
            path: "objects/42/name",
            type: "objects",
            body: { name: "Ancient gate" },
            invoke: (sut: TypesStore) => sut.setObjectName(42, "Ancient gate"),
        },
        {
            name: "varp bit",
            path: "varp-bits/42",
            type: "varp-bits",
            body: { configId: 100, bitLength: 3, bitOffset: 2 },
            invoke: (sut: TypesStore) => sut.setVarpBit(42, 100, 3, 2),
        },
        {
            name: "config definition",
            path: "config-definitions/42",
            type: "config-definitions",
            body: { defaultValue: -1, valueType: "i" },
            invoke: (sut: TypesStore) => sut.setConfigDefinition(42, -1, "i"),
        },
    ])("saves $name and publishes the server confirmation", async ({ path, type, body, invoke }) => {
        const result: TypeMutationResultDto = { operation: "updated", type, id: 42 };
        const pending = invoke(store);
        expect(store.loading()).toBe(true);
        expect(store.mutationResult()).toBeNull();
        const request = http.expectOne(`${baseUrl}${path}`);
        expect(request.request.method).toBe("POST");
        expect(request.request.body).toEqual(body);
        request.flush(result);
        await pending;
        expect(store.mutationResult()).toEqual(result);
        expect(store.loading()).toBe(false);
        expect(store.error()).toBeNull();
    });

    it("ignores a mutation while a read is pending and accepts it after completion", async () => {
        const pending = store.loadById("items", 42);
        await store.setObjectName(7, "Gate");
        http.expectNone(`${baseUrl}objects/7/name`);
        expect(store.loading()).toBe(true);
        expect(store.mutationResult()).toBeNull();

        http.expectOne(`${baseUrl}items/42`).flush({ id: 42 });
        await pending;
        const mutation = store.setObjectName(7, "Gate");
        const result = { operation: "updated", type: "objects", id: 7 };
        http.expectOne(`${baseUrl}objects/7/name`).flush(result);
        await mutation;
        expect(store.mutationResult()).toEqual(result);
        expect(store.loading()).toBe(false);
    });

    it("reports a failed lookup and clears the error when retrying", async () => {
        const failed = store.loadById("items", 404);
        http.expectOne(`${baseUrl}items/404`).flush({ detail: "Definition not found.", title: "Not found" }, { status: 404, statusText: "Not Found" });
        await failed;
        expect(store.error()).toBe("Definition not found.");
        expect(store.loading()).toBe(false);
        expect(store.byIdResult()).toBeNull();

        const retry = store.loadById("items", 42);
        expect(store.loading()).toBe(true);
        expect(store.error()).toBeNull();
        http.expectOne(`${baseUrl}items/42`).flush({ id: 42, name: "Bronze sword" });
        await retry;
        expect(store.byIdResult()).toEqual({ id: 42, name: "Bronze sword" });
        expect(store.loading()).toBe(false);
    });

    it("keeps loaded data and the last confirmation when a later mutation fails", async () => {
        const read = store.loadById("npcs", 42);
        http.expectOne(`${baseUrl}npcs/42`).flush({ id: 42, combatLevel: 10 });
        await read;
        const saved = { operation: "updated", type: "npcs", id: 42 };
        const save = store.setNpcCombatLevel(42, 25);
        http.expectOne(`${baseUrl}npcs/42/combat-level`).flush(saved);
        await save;

        const failed = store.setNpcCombatLevel(42, 30);
        http.expectOne(`${baseUrl}npcs/42/combat-level`).flush({ title: "Cache is read-only." }, { status: 409, statusText: "Conflict" });
        await failed;
        expect(store.error()).toBe("Cache is read-only.");
        expect(store.loading()).toBe(false);
        expect(store.byIdResult()).toEqual({ id: 42, combatLevel: 10 });
        expect(store.mutationResult()).toEqual(saved);
    });

    it("keeps state independent between separately provided stores", async () => {
        const pageInjector = createEnvironmentInjector([CacheTypesStore], TestBed.inject(EnvironmentInjector));
        try {
            const other = pageInjector.get(CacheTypesStore);
            const first = store.loadById("items", 42);
            http.expectOne(`${baseUrl}items/42`).flush({ id: 42 });
            await first;
            expect(other.byIdResult()).toBeNull();
            expect(other.loading()).toBe(false);

            const second = other.loadById("items", 7);
            http.expectOne(`${baseUrl}items/7`).flush({ id: 7 });
            await second;
            expect(store.byIdResult()).toEqual({ id: 42 });
            expect(other.byIdResult()).toEqual({ id: 7 });
        } finally {
            pageInjector.destroy();
        }
    });
});
