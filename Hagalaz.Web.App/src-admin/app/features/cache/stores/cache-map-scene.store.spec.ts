import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../environments/environment";
import type { MapPlacement, MapRenderRegion, ObjectRenderAppearance } from "../services/map-render.models";
import { CacheMapSceneStore } from "./cache-map-scene.store";

const base = environment.cacheApiUrl;
const batchUrl = `${base}types/objects/render-appearances`;
const region = (id: number): MapRenderRegion => ({
    id,
    regionX: id >> 8,
    regionY: id & 255,
    terrainStatus: "available",
    landscapeStatus: "available",
    planes: [],
    floors: [],
    objects: [],
    warnings: [],
});
const placement = (id = 1, plane = 0): MapPlacement => ({ id, plane, shape: 10, rotation: 0, x: 0, y: 0 });
const appearance: ObjectRenderAppearance = {
    id: 1,
    shape: 10,
    rotation: 0,
    sizeX: 1,
    sizeY: 1,
    decorDisplacement: 64,
    contourMode: 0,
    contourParameter: 0,
    parts: [],
    warnings: [],
};
const success = (key: Pick<MapPlacement, "id" | "shape" | "rotation">) => ({ ...key, status: 200, appearance: { ...appearance, ...key }, problem: null });
function textured(id: number, texture = 7): MapRenderRegion {
    return {
        ...region(id),
        floors: [{ id: 1, overlay: false, rgb: 1, textureId: texture, textureScale: 512, secondaryRgb: null }],
        planes: [{ cornerHeights: [], flags: [], underlays: [1], overlays: [], overlayShapes: [], overlayRotations: [] }],
    };
}

describe("CacheMapSceneStore", () => {
    let http: HttpTestingController;
    const landscapeUrl = (id: number) => `${base}types/maps/${id}/landscape`;
    const flushed = () => vi.advanceTimersByTimeAsync(0);

    it("saves each region in order and reports success", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const objects = [placement(4)];
        const result = store.saveRegions([{ kind: "landscape", regionId: 1, objects }, { kind: "landscape", regionId: 2, objects: [] }]);
        expect(store.saving()).toBe(true);
        const first = http.expectOne(landscapeUrl(1));
        expect(first.request.body).toEqual({ objects });
        http.expectNone(landscapeUrl(2));
        first.flush({});
        await flushed();
        http.expectOne(landscapeUrl(2)).flush({});
        await expect(result).resolves.toBe(true);
        expect(store.saving()).toBe(false);
        expect(store.saveError()).toBeNull();
    });

    it("stops at the first failure and names what was saved", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const result = store.saveRegions([{ kind: "landscape", regionId: 1, objects: [] }, { kind: "landscape", regionId: 2, objects: [] }, { kind: "landscape", regionId: 3, objects: [] }]);
        http.expectOne(landscapeUrl(1)).flush({});
        await flushed();
        http.expectOne(landscapeUrl(2)).flush({ detail: "Landscape is encrypted." }, { status: 422, statusText: "Unprocessable" });
        await expect(result).resolves.toBe(false);
        http.expectNone(landscapeUrl(3));
        expect(store.saving()).toBe(false);
        expect(store.saveError()).toBe("Could not save region 2 objects. Landscape is encrypted. Already saved: region 1 objects.");

        const retry = store.saveRegions([{ kind: "landscape", regionId: 2, objects: [] }]);
        expect(store.saveError()).toBeNull();
        http.expectOne(landscapeUrl(2)).flush({});
        await expect(retry).resolves.toBe(true);
    });

    it("saves a region's terrain before its objects", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const tiles = [{ plane: 0, x: 1, y: 2, height: 4 }];
        const result = store.saveRegions([{ kind: "terrain", regionId: 1, tiles }, { kind: "landscape", regionId: 1, objects: [] }]);
        const terrain = http.expectOne(`${base}types/maps/1/terrain`);
        expect(terrain.request.body).toEqual({ tiles });
        http.expectNone(landscapeUrl(1));
        terrain.flush({});
        await flushed();
        http.expectOne(landscapeUrl(1)).flush({});
        await expect(result).resolves.toBe(true);
    });

    it("refreshes the world map and reports a failure separately", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const url = `${base}types/maps/worldmap/refresh`;
        const result = { refreshed: [1], unchanged: [], skipped: [] };
        const done = store.refreshWorldMap([1]);
        expect(store.worldMapRefreshing()).toBe(true);
        await expect(store.refreshWorldMap([2])).resolves.toBeNull();
        const request = http.expectOne(url);
        expect(request.request.body).toEqual({ regions: [1] });
        request.flush(result);
        await expect(done).resolves.toEqual(result);
        expect(store.worldMapRefreshing()).toBe(false);

        const failed = store.refreshWorldMap([1]);
        http.expectOne(url).flush({ detail: "Unavailable." }, { status: 500, statusText: "Error" });
        await expect(failed).resolves.toBeNull();
        expect(store.worldMapError()).toBe("The world map was not updated. Unavailable.");
        expect(store.saveError()).toBeNull();
    });

    it("loads the floor catalog once and reports a failure", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const failed = store.loadFloors();
        expect(store.floorsLoading()).toBe(true);
        http.expectOne(`${base}types/maps/floors`).flush({ detail: "Unavailable." }, { status: 500, statusText: "Error" });
        await failed;
        expect(store.floorsError()).toBe("Unavailable.");
        expect(store.floorsLoading()).toBe(false);

        const floor = { id: 3, overlay: false, rgb: 1, textureId: -1, secondaryRgb: null, textureScale: 512 };
        const loaded = store.loadFloors();
        http.expectOne(`${base}types/maps/floors`).flush([floor]);
        await loaded;
        expect(store.floors()).toEqual([floor]);
        await store.loadFloors();
        http.expectNone(`${base}types/maps/floors`);
    });

    it("loads textures for painted floors that the stored terrain does not use", () => {
        const store = TestBed.inject(CacheMapSceneStore);
        store.setVisibility({ planes: [true], landscape: false });
        store.load({ id: 1, width: 1, height: 1, keys: {} });
        http.expectOne(`${base}types/maps/1/render`).flush(region(1));
        store.setVisibility({ planes: [true], landscape: false, previewTextures: [9] });
        http.expectOne((request) => request.url === `${base}textures/9/image?sourceAlpha=true`).error(new ProgressEvent("error"));
    });

    it("ignores a save while another is running", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const first = store.saveRegions([{ kind: "landscape", regionId: 1, objects: [] }]);
        await expect(store.saveRegions([{ kind: "landscape", regionId: 9, objects: [] }])).resolves.toBe(false);
        http.expectNone(landscapeUrl(9));
        http.expectOne(landscapeUrl(1)).flush({});
        await expect(first).resolves.toBe(true);
    });

    beforeEach(() => {
        vi.useFakeTimers();
        TestBed.configureTestingModule({ providers: [CacheMapSceneStore, provideHttpClient(withXhr()), provideHttpClientTesting()] });
        http = TestBed.inject(HttpTestingController);
        vi.stubGlobal(
            "createImageBitmap",
            vi.fn(async () => ({ close: vi.fn() }))
        );
    });
    afterEach(() => {
        http.verify();
        TestBed.resetTestingModule();
        vi.useRealTimers();
        vi.unstubAllGlobals();
    });
    function texture(id: number, material = { alphaMode: 0 }) {
        http.expectOne(`${base}textures/${id}/image?sourceAlpha=true`).flush(new Blob(["fixture"], { type: "image/png" }), {
            headers: { "X-Texture-Material": JSON.stringify(material) },
        });
        http.expectNone(`${base}textures/${id}/material`);
    }

    it("loads complete texture material settings with one image request", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        http.expectOne(`${base}types/maps/258/render`).flush(textured(258));
        const material = {
            alphaMode: 1,
            colorBlend: 170,
            brightness: 25,
            effectId: 2,
            effectParameter: 12,
            effectParameter2: 305419896,
            scrollU: -1,
            scrollV: 4,
            repeatU: true,
            repeatV: false,
            hasHdr: true,
        };
        texture(7, material);
        await vi.advanceTimersByTimeAsync(0);
        expect(store.textures().get(7)).toMatchObject(material);
        expect(store.textures().get(7)?.image).toBeDefined();
        expect(store.loading()).toBe(false);
        expect(store.progress()).toMatchObject({ texturesSettled: 1, texturesTotal: 1, failures: 0 });
    });

    it("scans shared appearance texture faces once per selection across arrivals and visibility changes", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const scans = vi.fn(() => [7,7,8,-1]);
        const row = {
            ...success(placement()),
            appearance: {
                ...appearance,
                parts: [{
                    id: 1, positions: [0,0,0,1,0,0,0,1,0], indices: [0,1,2],
                    faceColors: [127], faceAlpha: [0], faceTextureUvs: [0,0,1,0,0,1], warnings: [],
                    get faceTextureIds() { return scans(); },
                }],
            },
        };
        for (const id of [258,514]) {
            store.load({ id, width: 1, height: 1, keys: {} });
            const source = region(id);
            source.objects = Array.from({ length: 50 }, (_, x) => ({ ...placement(), x }));
            http.expectOne(`${base}types/maps/${id}/render`).flush(source);
            const batch = http.expectOne(batchUrl);
            expect(batch.request.body).toEqual([{ id: 1, shape: 10, rotation: 0 }]);
            batch.flush([row]);
            texture(7); texture(8);
            await vi.advanceTimersByTimeAsync(250);
            expect(store.loading()).toBe(false);
            expect(store.progress()).toMatchObject({ appearancesSettled: 1, appearancesTotal: 1, texturesSettled: 2, texturesTotal: 2, failures: 0 });
            store.setVisibility({ planes: [true], landscape: false });
            store.setVisibility({ planes: [true], landscape: true });
            http.expectNone(() => true);
            expect(store.textures().size).toBe(2);
            expect(scans).toHaveBeenCalledTimes(id === 258 ? 1 : 2);
        }
    });

    it("reports missing image material metadata without a second request or bitmap decode", () => {
        const store = TestBed.inject(CacheMapSceneStore);
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        http.expectOne(`${base}types/maps/258/render`).flush(textured(258));
        http.expectOne(`${base}textures/7/image?sourceAlpha=true`).flush(new Blob(["fixture"]));
        http.expectNone(`${base}textures/7/material`);
        expect(createImageBitmap).not.toHaveBeenCalled();
        expect(store.loading()).toBe(false);
        expect(store.progress()).toMatchObject({ texturesSettled: 1, failures: 1 });
        expect(store.warnings()).toContain("Texture 7: Texture image or material metadata is missing.");
    });

    it("loads the anchor region's environment-map faces but not a neighbour's", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        store.setVisibility({ planes: [true], landscape: false });
        store.load({ id: 258, width: 2, height: 1, keys: {} });
        const atmosphere = { lightRgb: 0xffffff, intensity: 1, positiveResponse: 1, negativeResponse: 1, directionX: 0, directionY: -1, directionZ: 0, skyRgb: 0xc8c0a8, fogValue: 0 };
        http.expectOne(`${base}types/maps/514/render`).flush({ ...region(514), atmosphere: { ...atmosphere, environmentFaces: [90, 91, 92, 93, 94, 95] } });
        http.expectOne(`${base}types/maps/258/render`).flush({ ...region(258), atmosphere: { ...atmosphere, environmentFaces: [20, 21, 22, 23, 24, 20] } });
        for (const id of [20, 21, 22, 23]) texture(id);
        await vi.advanceTimersByTimeAsync(0);
        texture(24);
        await vi.advanceTimersByTimeAsync(0);
        http.expectNone(`${base}textures/90/image?sourceAlpha=true`);
        expect(store.textures().size).toBe(0);
        expect(store.environmentFaces()).toHaveLength(6);
        expect(store.environmentFaces()?.[5]).toBe(store.environmentFaces()?.[0]);
        expect(store.progress()).toMatchObject({ texturesSettled: 5, texturesTotal: 5, failures: 0 });
    });

    it("withholds the cube until every face loads and publishes a face a visible floor also uses", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        store.setVisibility({ planes: [false], landscape: false });
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        const atmosphere = { lightRgb: 0xffffff, intensity: 1, positiveResponse: 1, negativeResponse: 1, directionX: 0, directionY: -1, directionZ: 0, skyRgb: 0xc8c0a8, fogValue: 0, environmentFaces: [7, 21, 22, 23, 24, 25] };
        http.expectOne(`${base}types/maps/258/render`).flush({ ...textured(258), atmosphere });
        for (const id of [7, 21, 22, 23]) texture(id);
        await vi.advanceTimersByTimeAsync(0);
        texture(24);
        await vi.advanceTimersByTimeAsync(250);
        expect(store.environmentFaces()).toBeNull();
        http.expectOne(`${base}textures/25/image?sourceAlpha=true`).flush(new Blob(["fixture"]), { status: 404, statusText: "Missing" });
        await vi.advanceTimersByTimeAsync(250);
        expect(store.environmentFaces()).toBeNull();
        expect(store.textures().size).toBe(0);
        expect(store.progress()).toMatchObject({ failures: 1 });

        store.setVisibility({ planes: [true], landscape: false });
        await vi.advanceTimersByTimeAsync(250);
        expect([...store.textures().keys()]).toEqual([7]);
        http.expectNone(`${base}textures/7/image?sourceAlpha=true`);
    });

    it("publishes a region and its floor textures while another region and landscape batch remain pending", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        store.load({ id: 258, width: 2, height: 1, keys: { 258: [1, 2, 3, 4] } });
        const left = http.expectOne(`${base}types/maps/258/render`),
            right = http.expectOne(`${base}types/maps/514/render`);
        expect(left.request.body).toEqual({ xteaKeys: [1, 2, 3, 4] });
        const first = textured(258);
        first.objects = [placement(), { ...placement(), x: 1 }];
        left.flush(first);
        expect(store.regions()).toEqual([first]);
        expect(store.loading()).toBe(true);
        const batch = http.expectOne(batchUrl);
        expect(batch.request.body).toEqual([{ id: 1, shape: 10, rotation: 0 }]);
        texture(7);
        await vi.advanceTimersByTimeAsync(250);
        expect(store.textures().size).toBe(1);
        expect(store.appearances().size).toBe(0);
        expect(store.loading()).toBe(true);
        const next = region(514);
        next.objects = [placement()];
        right.flush(next);
        http.expectNone(batchUrl);
        batch.flush([success(placement())]);
        expect(store.regions()).toHaveLength(2);
        expect(store.appearances().size).toBe(1);
        expect(store.loading()).toBe(false);
        expect(store.progress()).toMatchObject({ regionsSettled: 2, regionsTotal: 2, appearancesSettled: 1, appearancesTotal: 1, failures: 0 });
    });

    it("defers hidden planes and loads newly visible assets once without reloading regions", () => {
        const store = TestBed.inject(CacheMapSceneStore);
        store.setVisibility({ planes: [true, false], landscape: false });
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        const source = region(258);
        source.objects = [placement(1), placement(2, 1)];
        http.expectOne(`${base}types/maps/258/render`).flush(source);
        http.expectNone(batchUrl);
        expect(store.loading()).toBe(false);
        store.setVisibility({ planes: [true, false], landscape: true });
        const ground = http.expectOne(batchUrl);
        expect(ground.request.body).toEqual([{ id: 1, shape: 10, rotation: 0 }]);
        ground.flush([success(placement(1))]);
        store.setVisibility({ planes: [true, true], landscape: true });
        const upper = http.expectOne(batchUrl);
        expect(upper.request.body).toEqual([{ id: 2, shape: 10, rotation: 0 }]);
        upper.flush([success(placement(2, 1))]);
        store.setVisibility({ planes: [true, false], landscape: false });
        store.setVisibility({ planes: [true, true], landscape: true });
        http.expectNone(() => true);
        expect(store.appearances().size).toBe(2);
        expect(store.loading()).toBe(false);
    });

    it("bounds batches and concurrency and drops unstarted hidden work during loading", () => {
        const store = TestBed.inject(CacheMapSceneStore);
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        const source = region(258);
        source.objects = Array.from({ length: 70 }, (_, id) => placement(id));
        http.expectOne(`${base}types/maps/258/render`).flush(source);
        const batches = http.match(batchUrl);
        expect(batches).toHaveLength(2);
        expect(batches.map((batch) => batch.request.body.length)).toEqual([32, 32]);
        store.setVisibility({ planes: [true], landscape: false });
        for (const batch of batches) batch.flush(batch.request.body.map(success));
        http.expectNone(batchUrl);
        expect(store.loading()).toBe(false);
        store.setVisibility({ planes: [true], landscape: true });
        const remainder = http.expectOne(batchUrl);
        expect(remainder.request.body).toHaveLength(6);
        remainder.flush(remainder.request.body.map(success));
        expect(store.appearances().size).toBe(70);
    });

    it("defers hidden floor textures and bounds image work ahead of object textures", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const source = textured(258, 10);
        source.objects = [placement()];
        source.planes[0].underlays = [1, 2, 3, 4, 5, 6];
        source.floors = Array.from({ length: 7 }, (_, index) => ({
            id: index + 1,
            overlay: false,
            rgb: 1,
            textureId: index + 10,
            textureScale: 512,
            secondaryRgb: null,
        }));
        source.planes.push({ ...source.planes[0], underlays: [7] });
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        http.expectOne(`${base}types/maps/258/render`).flush(source);
        const fourth = http.expectOne(`${base}textures/13/image?sourceAlpha=true`);
        http.expectNone(`${base}textures/14/image?sourceAlpha=true`);
        http.expectNone(`${base}textures/16/image?sourceAlpha=true`);
        const object = success(placement());
        http.expectOne(batchUrl).flush([
            {
                ...object,
                appearance: {
                    ...object.appearance,
                    parts: [
                        {
                            id: 1,
                            positions: [0, 0, 0, 1, 0, 0, 0, 1, 0],
                            indices: [0, 1, 2],
                            faceColors: [127],
                            faceAlpha: [0],
                            faceTextureIds: [100],
                            faceTextureUvs: [0, 0, 1, 0, 0, 1],
                            warnings: [],
                        },
                    ],
                },
            },
        ]);
        http.expectNone(`${base}textures/100/image?sourceAlpha=true`);
        texture(10);
        await vi.advanceTimersByTimeAsync(0);
        http.expectNone(`${base}textures/100/image?sourceAlpha=true`);
        texture(11);
        await vi.advanceTimersByTimeAsync(0);
        http.expectNone(`${base}textures/100/image?sourceAlpha=true`);
        texture(12);
        fourth.flush(new Blob(["fixture"]), { headers: { "X-Texture-Material": JSON.stringify({ alphaMode: 0 }) } });
        texture(14);
        texture(15);
        await vi.advanceTimersByTimeAsync(0);
        texture(100);
        await vi.advanceTimersByTimeAsync(0);
        expect(store.textures().size).toBe(7);
        store.setVisibility({ planes: [true, true], landscape: true });
        texture(16);
        await vi.advanceTimersByTimeAsync(0);
        expect(store.textures().size).toBe(8);
        store.setVisibility({ planes: [true, false], landscape: true });
        store.setVisibility({ planes: [true, true], landscape: true });
        http.expectNone(() => true);
    });

    it("coalesces partial publications and flushes the last batch immediately", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        const source = region(258);
        source.objects = Array.from({ length: 65 }, (_, id) => placement(id));
        http.expectOne(`${base}types/maps/258/render`).flush(source);
        const batches = http.match(batchUrl);
        batches[0].flush(batches[0].request.body.map(success));
        const final = http.expectOne(batchUrl);
        expect(store.appearances().size).toBe(0);
        await vi.advanceTimersByTimeAsync(249);
        expect(store.appearances().size).toBe(0);
        await vi.advanceTimersByTimeAsync(1);
        expect(store.appearances().size).toBe(32);
        batches[1].flush(batches[1].request.body.map(success));
        expect(store.appearances().size).toBe(32);
        final.flush(final.request.body.map(success));
        expect(store.appearances().size).toBe(65);
        expect(store.loading()).toBe(false);
    });

    it("keeps partial region and appearance failures visible and retries without duplicate content", () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const request = { id: 258, width: 1, height: 2, keys: {} };
        store.load(request);
        const valid = region(258);
        valid.objects = [placement(1), placement(2)];
        http.expectOne(`${base}types/maps/258/render`).flush(valid);
        http.expectOne(`${base}types/maps/259/render`).flush({ detail: "Read failed" }, { status: 500, statusText: "Failure" });
        http.expectOne(batchUrl).flush([
            success(placement(1)),
            { id: 2, shape: 10, rotation: 0, status: 422, appearance: null, problem: { detail: "Unsupported model" } },
        ]);
        expect(store.regions()).toEqual([valid]);
        expect(store.appearances().size).toBe(1);
        expect(store.warnings()).toEqual(expect.arrayContaining(["Region 259: Read failed", "Object 2, shape 10: Unsupported model"]));
        expect(store.progress().failures).toBe(2);
        store.load(request);
        http.expectOne(`${base}types/maps/258/render`).flush(region(258));
        http.expectOne(`${base}types/maps/259/render`).flush(region(259));
        expect(store.regions()).toHaveLength(2);
        expect(store.appearances().size).toBe(0);
        expect(store.warnings()).toEqual([]);
        expect(store.loading()).toBe(false);
    });

    it("rejects invalid rectangles and cancels superseded HTTP work and scheduled publications", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        store.load({ id: 65535, width: 2, height: 1, keys: {} });
        expect(store.warnings()[0]).toContain("valid region rectangle");
        http.expectNone(() => true);
        store.load({ id: 258, width: 1, height: 2, keys: {} });
        const oldRegion = http.expectOne(`${base}types/maps/259/render`);
        const source = region(258);
        source.objects = [placement()];
        http.expectOne(`${base}types/maps/258/render`).flush(source);
        const oldBatch = http.expectOne(batchUrl);
        store.load({ id: 260, width: 1, height: 1, keys: {} });
        expect(oldRegion.cancelled).toBe(true);
        expect(oldBatch.cancelled).toBe(true);
        http.expectOne(`${base}types/maps/260/render`).flush(region(260));
        await vi.advanceTimersByTimeAsync(1000);
        expect(store.regions().map((region) => region.id)).toEqual([260]);
        expect(store.appearances().size).toBe(0);
    });

    it("owns decoded bitmaps until replacement and closes late completions", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const bitmap = { close: vi.fn() };
        let resolve: (imageValue: typeof bitmap) => void = () => {
            throw new Error("Bitmap was not requested");
        };
        vi.stubGlobal(
            "createImageBitmap",
            vi.fn(
                () =>
                    new Promise<typeof bitmap>((done) => {
                        resolve = done;
                    })
            )
        );
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        http.expectOne(`${base}types/maps/258/render`).flush(textured(258));
        texture(7);
        resolve(bitmap);
        await vi.advanceTimersByTimeAsync(0);
        expect(store.textures().size).toBe(1);
        expect(bitmap.close).not.toHaveBeenCalled();
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        expect(bitmap.close).toHaveBeenCalledOnce();
        http.expectOne(`${base}types/maps/258/render`).flush(textured(258));
        texture(7);
        const late = { close: vi.fn() };
        TestBed.resetTestingModule();
        resolve(late);
        await vi.advanceTimersByTimeAsync(0);
        expect(late.close).toHaveBeenCalledOnce();
    });

    it("reports texture and batch request errors and finishes visible work", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        const source = textured(258);
        source.objects = [placement()];
        http.expectOne(`${base}types/maps/258/render`).flush(source);
        texture(7, { alphaMode: 9 });
        http.expectOne(batchUrl).flush({ detail: "Batch unavailable" }, { status: 503, statusText: "Failure" });
        await vi.advanceTimersByTimeAsync(0);
        expect(store.loading()).toBe(false);
        expect(store.progress()).toMatchObject({ failures: 2, appearancesSettled: 1, texturesSettled: 1 });
        expect(store.warnings()).toEqual(expect.arrayContaining(["Texture 7: Unsupported texture alpha mode.", "Object 1, shape 10: Batch unavailable"]));
    });
});
