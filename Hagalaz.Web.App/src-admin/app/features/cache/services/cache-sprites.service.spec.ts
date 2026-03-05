import { TestBed } from "@angular/core/testing";
import { provideHttpClient } from "@angular/common/http";
import { provideHttpClientTesting, HttpTestingController } from "@angular/common/http/testing";
import { describe, expect, it } from "vitest";
import { CacheSpritesService } from "./cache-sprites.service";

describe("CacheSpritesService", () => {
    it("requests sprite png as blob", () => {
        TestBed.configureTestingModule({
            providers: [provideHttpClient(), provideHttpClientTesting(), CacheSpritesService],
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
            providers: [provideHttpClient(), provideHttpClientTesting(), CacheSpritesService],
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
});
