import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { environment } from "../../../../environments/environment";
import type { SearchResultDto, SpriteMutationResultDto } from "../services/cache.models";
import { CacheSpritesStore } from "./cache-sprites.store";

const baseUrl = `${environment.cacheApiUrl}types/sprites`;

describe("CacheSpritesStore", () => {
    let store: InstanceType<typeof CacheSpritesStore>;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [provideHttpClient(withXhr()), provideHttpClientTesting(), CacheSpritesStore],
        });
        store = TestBed.inject(CacheSpritesStore);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it("starts without metadata, previews, confirmations, or errors", () => {
        expect({
            sprite: store.spriteInfo(),
            search: store.searchResult(),
            mutation: store.mutationResult(),
            blob: store.imageBlob(),
            description: store.imageDescription(),
            error: store.error(),
            loading: store.loading(),
        }).toEqual({ sprite: null, search: null, mutation: null, blob: null, description: "", error: null, loading: false });
        http.expectNone(() => true);
    });

    it("loads sprite metadata and clears loading after the response", async () => {
        const metadata = { id: 12, width: 32, height: 16, frameCount: 2 };
        const pending = store.loadSprite(12);
        expect(store.loading()).toBe(true);
        expect(store.spriteInfo()).toBeNull();
        const request = http.expectOne(`${baseUrl}/12`);
        expect(request.request.method).toBe("GET");
        request.flush(metadata);
        await pending;
        expect(store.spriteInfo()).toEqual(metadata);
        expect(store.loading()).toBe(false);
        expect(store.error()).toBeNull();
    });

    it.each([
        { name: "matching", query: "bronze sword", offset: 5, items: [{ id: 12 }], totalMatches: 20 },
        { name: "empty", query: "", offset: 0, items: [], totalMatches: 0 },
    ])("publishes $name sprite searches with the requested paging", async ({ query, offset, items, totalMatches }) => {
        const result: SearchResultDto<unknown> = { query, offset, limit: 10, totalMatches, items };
        const pending = store.searchSprites(query, offset, 10);
        const request = http.expectOne((req) => req.url === `${baseUrl}/search`);
        expect(request.request.params.get("query")).toBe(query);
        expect(request.request.params.get("offset")).toBe(String(offset));
        expect(request.request.params.get("limit")).toBe("10");
        request.flush(result);
        await pending;
        expect(store.searchResult()).toEqual(result);
        expect(store.loading()).toBe(false);
    });

    it("loads the requested PNG frame and publishes its matching description", async () => {
        const blob = new Blob([new Uint8Array([1, 2, 3])], { type: "image/png" });
        const pending = store.previewSpriteImage(12, 2);
        expect(store.loading()).toBe(true);
        expect(store.imageBlob()).toBeNull();
        const request = http.expectOne((req) => req.url === `${baseUrl}/12/image`);
        expect(request.request.responseType).toBe("blob");
        expect(request.request.params.get("frame")).toBe("2");
        request.flush(blob);
        await pending;
        expect(store.imageBlob()).toBe(blob);
        expect(store.imageDescription()).toBe("Sprite #12, frame 2");
        expect(store.loading()).toBe(false);
        expect(store.error()).toBeNull();
    });

    it("keeps the previous preview when the next image request fails", async () => {
        const blob = new Blob(["first"], { type: "image/png" });
        const first = store.previewSpriteImage(12, 0);
        http.expectOne((req) => req.url === `${baseUrl}/12/image`).flush(blob);
        await first;

        const failed = store.previewSpriteImage(99, 1);
        http.expectOne((req) => req.url === `${baseUrl}/99/image`).error(new ProgressEvent("error"));
        await failed;
        expect(store.imageBlob()).toBe(blob);
        expect(store.imageDescription()).toBe("Sprite #12, frame 0");
        expect(store.error()).toContain(`${baseUrl}/99/image`);
        expect(store.loading()).toBe(false);
    });

    it("uploads the selected PNG and publishes the server dimensions", async () => {
        const file = new File([new Uint8Array([1, 2, 3])], "sprite.png", { type: "image/png" });
        const result: SpriteMutationResultDto = { operation: "sprite-image-replaced", type: "sprite", id: 99, width: 32, height: 16, frameCount: 2 };
        const pending = store.uploadSpriteImage(99, file);
        expect(store.loading()).toBe(true);
        expect(store.mutationResult()).toBeNull();
        const request = http.expectOne(`${baseUrl}/99/image`);
        expect(request.request.method).toBe("POST");
        expect(request.request.body).toBeInstanceOf(FormData);
        expect((request.request.body as FormData).get("file")).toBe(file);
        request.flush(result);
        await pending;
        expect(store.mutationResult()).toEqual(result);
        expect(store.loading()).toBe(false);
        expect(store.error()).toBeNull();
    });

    it("rejects an upload without a file before sending a request", async () => {
        await store.uploadSpriteImage(99, null);
        http.expectNone(() => true);
        expect(store.error()).toBe("Select a PNG file before uploading.");
        expect(store.loading()).toBe(false);
        expect(store.mutationResult()).toBeNull();
    });

    it("clears an error without discarding loaded metadata", async () => {
        const pending = store.loadSprite(12);
        http.expectOne(`${baseUrl}/12`).flush({ id: 12 });
        await pending;
        await store.uploadSpriteImage(12, null);
        expect(store.error()).toBe("Select a PNG file before uploading.");
        store.clearError();
        expect(store.error()).toBeNull();
        expect(store.spriteInfo()).toEqual({ id: 12 });
    });

    it("ignores a preview while metadata is pending and accepts it afterward", async () => {
        const metadata = store.loadSprite(12);
        await store.previewSpriteImage(12, 0);
        http.expectNone((req) => req.url === `${baseUrl}/12/image`);
        expect(store.loading()).toBe(true);
        expect(store.imageBlob()).toBeNull();
        http.expectOne(`${baseUrl}/12`).flush({ id: 12 });
        await metadata;

        const blob = new Blob(["image"], { type: "image/png" });
        const preview = store.previewSpriteImage(12, 0);
        http.expectOne((req) => req.url === `${baseUrl}/12/image`).flush(blob);
        await preview;
        expect(store.imageBlob()).toBe(blob);
        expect(store.loading()).toBe(false);
    });

    it("reports a network failure and clears it when retrying", async () => {
        const failed = store.loadSprite(12);
        http.expectOne(`${baseUrl}/12`).error(new ProgressEvent("error"));
        await failed;
        expect(store.error()).toContain(`${baseUrl}/12`);
        expect(store.error()).toBe(`Http failure response for ${baseUrl}/12: 0 `);
        expect(store.loading()).toBe(false);
        expect(store.spriteInfo()).toBeNull();

        const retry = store.loadSprite(12);
        expect(store.error()).toBeNull();
        expect(store.loading()).toBe(true);
        http.expectOne(`${baseUrl}/12`).flush({ id: 12 });
        await retry;
        expect(store.spriteInfo()).toEqual({ id: 12 });
        expect(store.loading()).toBe(false);
    });
});
