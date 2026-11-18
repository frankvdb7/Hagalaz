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
    terrainVersion: 4,
    landscapeVersion: 9,
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

    it("saves regions in order and sends keys only for the keyed landscape", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const objects = [placement(4)];
        const xteaKeys = [1, 0, -3, 4];
        const result = store.saveRegions([{ kind: "landscape", regionId: 1, objects, expectedVersion: 9, xteaKeys }, { kind: "landscape", regionId: 2, objects: [], expectedVersion: 9 }]);
        expect(store.saving()).toBe(true);
        const first = http.expectOne(landscapeUrl(1));
        expect(first.request.body).toEqual({ objects, expectedVersion: 9, xteaKeys });
        http.expectNone(landscapeUrl(2));
        first.flush({});
        await flushed();
        const second = http.expectOne(landscapeUrl(2));
        expect(second.request.body).toEqual({ objects: [], expectedVersion: 9 });
        second.flush({});
        await expect(result).resolves.toMatchObject({ saved: expect.any(Array), unattempted: [] });
        expect(store.saving()).toBe(false);
        expect(store.saveError()).toBeNull();
    });

    it("stops at the first failure and names what was saved", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const result = store.saveRegions([{ kind: "landscape", regionId: 1, objects: [], expectedVersion: 9 }, { kind: "landscape", regionId: 2, objects: [], expectedVersion: 9 }, { kind: "landscape", regionId: 3, objects: [], expectedVersion: 9 }]);
        http.expectOne(landscapeUrl(1)).flush({});
        await flushed();
        http.expectOne(landscapeUrl(2)).flush({ detail: "Landscape is encrypted." }, { status: 422, statusText: "Unprocessable" });
        await expect(result).resolves.toMatchObject({ saved: [{ regionId: 1 }], failed: { regionId: 2 } });
        http.expectNone(landscapeUrl(3));
        expect(store.saving()).toBe(false);
        expect(store.saveError()).toContain("Could not save region 2 objects. Landscape is encrypted. Already saved: region 1 objects. Not attempted: region 3 objects.");

        const retry = store.saveRegions([{ kind: "landscape", regionId: 2, objects: [], expectedVersion: 9 }]);
        expect(store.saveError()).toBeNull();
        http.expectOne(landscapeUrl(2)).flush({});
        await expect(retry).resolves.toMatchObject({ saved: [{ regionId: 2 }], unattempted: [] });
    });

    it("reports a save refused for a stale version, keeps the earlier saves named and never retries it", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const result = store.saveRegions([{ kind: "landscape", regionId: 1, objects: [], expectedVersion: 9 }, { kind: "landscape", regionId: 2, objects: [], expectedVersion: 9 }]);
        http.expectOne(landscapeUrl(1)).flush({});
        await flushed();
        http.expectOne(landscapeUrl(2)).flush({ detail: "Region 2's landscape was changed by another save after it was loaded." }, { status: 409, statusText: "Conflict" });
        await expect(result).resolves.toMatchObject({ saved: [{ regionId: 1 }], failed: { regionId: 2 } });
        http.expectNone(landscapeUrl(2));
        expect(store.saveError()).toContain("Could not save region 2 objects. Region 2's landscape was changed by another save after it was loaded. Already saved: region 1 objects.");
    });

    it("saves a region's terrain before its objects", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const tiles = [{ plane: 0, x: 1, y: 2, height: 4 }];
        const result = store.saveRegions([{ kind: "terrain", regionId: 1, tiles, expectedVersion: 4 }, { kind: "landscape", regionId: 1, objects: [], expectedVersion: 9 }]);
        const terrain = http.expectOne(`${base}types/maps/1/terrain`);
        expect(terrain.request.body).toEqual({ tiles, expectedVersion: 4 });
        http.expectNone(landscapeUrl(1));
        terrain.flush({});
        await flushed();
        http.expectOne(landscapeUrl(1)).flush({});
        await expect(result).resolves.toMatchObject({ saved: expect.any(Array), unattempted: [] });
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

    it("updates a western underwater border from the owner's surface when that owner has no underwater plane", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const terrain = (height: number) => ({
            cornerHeights: Array<number>(65 * 65).fill(height),
            flags: [], underlays: [], overlays: [], overlayShapes: [], overlayRotations: [],
        });
        const surfaces = () => Array.from({ length: 4 }, () => terrain(-640));
        const west = { ...region(258), planes: surfaces(), underwaterPlane: terrain(-512) };
        const east = { ...region(514), planes: surfaces() };
        const border = 64 * 65 + 1;
        west.underwaterPlane.cornerHeights[border] = -640;
        store.load({ id: 258, width: 2, height: 1, keys: {} });
        http.expectOne(`${base}types/maps/258/render`).flush(west);
        http.expectOne(`${base}types/maps/514/render`).flush(east);

        const updated = terrain(-640);
        updated.cornerHeights[1] = -704;
        const reload = store.reloadRegion(514, ["terrain"]);
        http.expectOne(`${base}types/maps/514/render`).flush({ ...east, terrainVersion: 5, planes: [updated, ...east.planes.slice(1)] });
        await expect(reload).resolves.toBeNull();
        const westPlanes = store.regions()[0].planes;
        expect(westPlanes[0].cornerHeights[border]).toBe(-704);
        expect(westPlanes[4].cornerHeights[border]).toBe(-704);
        expect(westPlanes[4].cornerHeights[1]).toBe(-512);
        expect(store.regions()[0].terrainVersion).toBe(4);
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
        const first = store.saveRegions([{ kind: "landscape", regionId: 1, objects: [], expectedVersion: 9 }]);
        await expect(store.saveRegions([{ kind: "landscape", regionId: 9, objects: [], expectedVersion: 9 }])).resolves.toBeNull();
        http.expectNone(landscapeUrl(9));
        http.expectOne(landscapeUrl(1)).flush({});
        await expect(first).resolves.toMatchObject({ saved: [{ regionId: 1 }] });
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
        // Tests that do not exercise water leave the texture material batch unanswered.
        http.match(request => request.url.endsWith("textures/materials"));
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
        // Effect 2 is water: the texture and its ridged noise volume make up the water step.
        await vi.waitFor(async () => {
            await vi.advanceTimersByTimeAsync(250);
            expect(store.loading()).toBe(false);
        });
        expect(store.textures().get(7)).toMatchObject(material);
        expect(store.textures().get(7)?.image).toBeDefined();
        expect(store.progress()).toMatchObject({ texturesSettled: 0, texturesTotal: 0, waterSettled: 2, waterTotal: 2, failures: 0 });
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
            http.expectNone(request => !request.url.endsWith("textures/materials"));
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

    it("shares clips and duplicate frames, reporting animation reads as their own load step", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const objects = [placement(1), placement(2)];
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        http.expectOne(`${base}types/maps/258/render`).flush({ ...region(258), objects });
        http.expectOne(batchUrl).flush(objects.map(object => ({
            ...success(object),
            appearance: { ...appearance, ...object, parts: [{ animationIds: [42], objectAnimation: { rotation: 0 } }] },
        })));
        // Objects publish at the next progressive step while their animations load.
        await vi.advanceTimersByTimeAsync(250);
        expect(store.appearances().size).toBe(2);
        expect(store.loading()).toBe(true);
        expect(store.progress()).toMatchObject({ animationsSettled: 0, animationsTotal: 1 });
        expect(store.animations().size).toBe(0);
        // One shared clip, read once with its two distinct frames.
        const clips = http.expectOne(`${base}types/animations/clips`);
        expect(clips.request.body).toEqual([42]);
        const frame = { frame: { transforms: [] }, skeleton: { types: [], groups: [] } };
        clips.flush([{ id: 42, status: 200, sequence: { frameIds: [6, 5, 6], frameDurations: [1, 2, 3], loop: 2 }, frames: [{ frameId: 6, frame }, { frameId: 5, frame }], problem: null }]);
        expect(store.animations().get(42)?.sequence.frameIds).toEqual([6, 5, 6]);
        expect(store.animations().get(42)?.frames.size).toBe(2);
        expect(store.loading()).toBe(false);
        expect(store.progress()).toMatchObject({ animationsSettled: 1, animationsTotal: 1 });
    });

    it("keeps a failed animation in the base pose and cancels old frames on selection change", () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const objects = [placement(1), placement(2)];
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        http.expectOne(`${base}types/maps/258/render`).flush({ ...region(258), objects });
        http.expectOne(batchUrl).flush(objects.map(object => ({
            ...success(object),
            appearance: { ...appearance, ...object, parts: [{ animationIds: [object.id], objectAnimation: { rotation: 0 } }] },
        })));
        const pending = http.expectOne(`${base}types/animations/clips`);
        expect(pending.request.body).toEqual([1, 2]);
        store.load({ id: 259, width: 1, height: 1, keys: {} });
        expect(pending.cancelled).toBe(true);
        http.expectOne(`${base}types/maps/259/render`).flush(region(259));
        expect(store.animations().size).toBe(0);
        expect(store.warnings()).toEqual([]);
    });

    it("keeps animations that fail on their own row, or with no frames, in the base pose", () => {
        const store = TestBed.inject(CacheMapSceneStore);
        const objects = [placement(1), placement(2), placement(3)];
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        http.expectOne(`${base}types/maps/258/render`).flush({ ...region(258), objects });
        http.expectOne(batchUrl).flush(objects.map(object => ({
            ...success(object),
            appearance: { ...appearance, ...object, parts: [{ animationIds: [object.id], objectAnimation: { rotation: 0 } }] },
        })));
        const frame = { frame: { transforms: [] }, skeleton: { types: [], groups: [] } };
        http.expectOne(`${base}types/animations/clips`).flush([
            { id: 1, status: 200, sequence: { frameIds: [] }, frames: [], problem: null },
            { id: 2, status: 404, sequence: null, frames: null, problem: { title: "Not found", detail: "Frame 55 is absent." } },
            { id: 3, status: 200, sequence: { frameIds: [9] }, frames: [{ frameId: 9, frame }], problem: null },
        ]);
        expect(store.warnings()).toEqual(expect.arrayContaining(["Animation 1: Sequence has no frames.", "Animation 2: Frame 55 is absent."]));
        expect([...store.animations().keys()]).toEqual([3]);
        expect(store.appearances().size).toBe(3);
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

    it("loads water textures as their own phase once materials name them, counting their noise volume", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        const source = textured(258);
        // Eight floor textures: six fill the image slots, then 16 (ordinary) and 17 (water) wait in that order.
        source.floors = [10, 11, 12, 13, 14, 15, 16, 17].map(id => ({ id, overlay: false, rgb: 1, textureId: id, textureScale: 512, secondaryRgb: null }));
        source.planes[0].underlays = [10, 11, 12, 13, 14, 15, 16, 17];
        http.expectOne(`${base}types/maps/258/render`).flush(source);
        const materials = http.expectOne(`${base}textures/materials`);
        expect(materials.request.body).toEqual([10, 11, 12, 13, 14, 15, 16, 17]);
        const material = (effectId: number) => ({ alphaMode: 0, colorBlend: 0, brightness: 0, effectId, effectParameter: 0, scrollU: 0, scrollV: 0, repeatU: true, repeatV: true, hasHdr: false, effectParameter2: 0 });
        materials.flush([10, 11, 12, 13, 14, 15, 16, 17].map(id => ({ id, status: 200, material: material(id === 17 ? 2 : 0) })));
        await vi.advanceTimersByTimeAsync(250);
        // Texture 17 and the ridged noise volume it needs.
        expect(store.progress()).toMatchObject({ waterTotal: 2, texturesTotal: 7 });
        texture(10);
        await vi.advanceTimersByTimeAsync(0);
        http.expectOne(`${base}textures/17/image?sourceAlpha=true`);
        http.expectNone(`${base}textures/16/image?sourceAlpha=true`);
        http.match(request => /textures\/\d+\/image/.test(request.url));
    });

    it("backs off progressive publications so rebuilds do not starve loading", async () => {
        const store = TestBed.inject(CacheMapSceneStore);
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        const source = textured(258);
        source.floors = [7, 8, 9].map(id => ({ id, overlay: false, rgb: 1, textureId: id, textureScale: 512, secondaryRgb: null }));
        source.planes[0].underlays = [7, 8, 9];
        source.objects = [placement()];
        http.expectOne(`${base}types/maps/258/render`).flush(source);
        // The landscape batch stays pending, so each texture arrival waits for a progressive publication.
        http.expectOne(batchUrl);
        const publishedAfter = async (id: number, wait: number) => {
            texture(id);
            await vi.advanceTimersByTimeAsync(wait - 1);
            const before = store.textures().has(id);
            await vi.advanceTimersByTimeAsync(1);
            return [before, store.textures().has(id)];
        };
        expect(await publishedAfter(7, 250)).toEqual([false, true]);
        expect(await publishedAfter(8, 500)).toEqual([false, true]);
        expect(await publishedAfter(9, 1000)).toEqual([false, true]);
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
        // Publications wait 250 ms for each selected region, so two regions publish after 500 ms.
        await vi.advanceTimersByTimeAsync(499);
        expect(store.textures().size).toBe(0);
        await vi.advanceTimersByTimeAsync(1);
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
        http.expectNone(request => !request.url.endsWith("textures/materials"));
        expect(store.appearances().size).toBe(2);
        expect(store.loading()).toBe(false);
    });

    it("requests the focused plane's floors and objects first, then the planes nearest to it", () => {
        const store = TestBed.inject(CacheMapSceneStore);
        store.setVisibility({ planes: [true, true, true, true], landscape: true, focusPlane: 2 });
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        const source = region(258);
        const level = (underlay: number) => ({ cornerHeights: [], flags: [], underlays: [underlay], overlays: [], overlayShapes: [], overlayRotations: [] });
        source.planes = [level(1), level(2), level(3), level(4)];
        source.floors = [1, 2, 3, 4].map(id => ({ id, overlay: false, rgb: 1, textureId: 10 + id, textureScale: 512, secondaryRgb: null }));
        source.objects = [1, 2, 3, 4, 5, 6, 7, 8].map(id => placement(id, (id - 1) % 4));
        http.expectOne(`${base}types/maps/258/render`).flush(source);

        const textures = http.match(request => /textures\/\d+\/image/.test(request.url)).map(request => Number(request.request.url.match(/textures\/(\d+)/)?.[1]));
        expect(textures).toEqual([13, 12, 14, 11]);
        const batch = http.expectOne(batchUrl);
        expect(batch.request.body.map((key: MapPlacement) => key.id)).toEqual([3, 7, 2, 4, 6, 8, 1, 5]);
    });

    it("requests objects nearest the camera's focus tile first within each plane", () => {
        const store = TestBed.inject(CacheMapSceneStore);
        // Region 258 spans world tiles 64-127 by 128-191; focus near its north-east corner.
        store.setVisibility({ planes: [true, true], landscape: true, focusPlane: 0, focusTile: { x: 126, y: 190 } });
        store.load({ id: 258, width: 1, height: 1, keys: {} });
        const source = region(258);
        const at = (id: number, plane: number, x: number, y: number): MapPlacement => ({ ...placement(id, plane), x, y });
        source.objects = [at(1, 0, 0, 0), at(2, 1, 63, 63), at(3, 0, 60, 60), at(4, 0, 30, 30)];
        http.expectOne(`${base}types/maps/258/render`).flush(source);
        expect(http.expectOne(batchUrl).request.body.map((key: MapPlacement) => key.id)).toEqual([3, 4, 1, 2]);
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
        // Six concurrent image reads take every visible floor texture; the hidden plane's texture 16 waits.
        const sixth = http.expectOne(`${base}textures/15/image?sourceAlpha=true`);
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
        texture(11);
        texture(12);
        texture(13);
        texture(14);
        sixth.flush(new Blob(["fixture"]), { headers: { "X-Texture-Material": JSON.stringify({ alphaMode: 0 }) } });
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
        http.expectNone(request => !request.url.endsWith("textures/materials"));
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
        http.expectNone(request => !request.url.endsWith("textures/materials"));
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
