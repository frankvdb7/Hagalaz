import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { describe, expect, it } from "vitest";
import { CacheSpritesService } from "./cache-sprites.service";

describe("CacheSpritesService", () => {
    it("requests sprite png as blob", () => {
        TestBed.configureTestingModule({
            providers: [provideHttpClient(withXhr()), provideHttpClientTesting(), CacheSpritesService],
        });

        const service = TestBed.inject(CacheSpritesService);
        const http = TestBed.inject(HttpTestingController);

        service.getSpriteImage(12, 0).subscribe();

        const request = http.expectOne((req) => req.url.includes("/types/sprites/12/image"));
        expect(request.request.responseType).toBe("blob");
        request.flush(new Blob(["x"], { type: "image/png" }));

        http.verify();
    });

    it("uploads sprite image using multipart form-data", () => {
        TestBed.configureTestingModule({
            providers: [provideHttpClient(withXhr()), provideHttpClientTesting(), CacheSpritesService],
        });

        const service = TestBed.inject(CacheSpritesService);
        const http = TestBed.inject(HttpTestingController);
        const file = new File([new Uint8Array([1, 2, 3])], "sprite.png", { type: "image/png" });

        service.replaceSpriteImage(99, file).subscribe();

        const request = http.expectOne((req) => req.url.includes("/types/sprites/99/image"));
        expect(request.request.method).toBe("POST");
        expect(request.request.body instanceof FormData).toBe(true);

        const uploaded = (request.request.body as FormData).get("file") as File;
        expect(uploaded.name).toBe("sprite.png");

        request.flush({ operation: "sprite-image-replaced", type: "sprite", id: 99, width: 1, height: 1, frameCount: 1 });
        http.verify();
    });
    it("shares decoded frame requests and invalidates them after a successful replacement", () => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(withXhr()), provideHttpClientTesting(), CacheSpritesService] });
        const service = TestBed.inject(CacheSpritesService);
        const http = TestBed.inject(HttpTestingController);
        const blobs: Blob[] = [];
        service.getSpriteImage(12, 0).subscribe((blob) => blobs.push(blob));
        service.getSpriteImage(12, 0).subscribe((blob) => blobs.push(blob));
        const original = new Blob(["original"], { type: "image/png" });
        http.expectOne((r) => r.url.endsWith("/sprites/12/image")).flush(original);
        service.getSpriteImage(12, 0).subscribe((blob) => blobs.push(blob));
        expect(blobs).toEqual([original, original, original]);
        http.expectNone(() => true);
        service.replaceSpriteImage(12, new File(["png"], "sprite.png", { type: "image/png" })).subscribe();
        http.expectOne((r) => r.method === "POST").flush({ operation: "sprite-image-replaced", type: "sprite", id: 12, width: 1, height: 1, frameCount: 1 });
        service.getSpriteImage(12, 0).subscribe((blob) => blobs.push(blob));
        const replacement = new Blob(["new"], { type: "image/png" });
        http.expectOne((r) => r.method === "GET").flush(replacement);
        expect(blobs.at(-1)).toBe(replacement);
        http.verify();
    });

    it("allows a failed frame request to be retried", () => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(withXhr()), provideHttpClientTesting(), CacheSpritesService] });
        const service = TestBed.inject(CacheSpritesService);
        const http = TestBed.inject(HttpTestingController);
        let failed = false;
        service.getSpriteImage(12, 0).subscribe({
            error: () => {
                failed = true;
            },
        });
        http.expectOne((r) => r.url.endsWith("/sprites/12/image")).error(new ProgressEvent("error"));
        expect(failed).toBe(true);
        let loaded: Blob | null = null;
        service.getSpriteImage(12, 0).subscribe((blob) => {
            loaded = blob;
        });
        const replacement = new Blob(["png"], { type: "image/png" });
        http.expectOne((r) => r.url.endsWith("/sprites/12/image")).flush(replacement);
        expect(loaded).toBe(replacement);
        http.verify();
    });
});
