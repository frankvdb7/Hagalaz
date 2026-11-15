import { provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { firstValueFrom } from "rxjs";
import { describe, expect, it } from "vitest";
import { CacheMapsService } from "./cache-maps.service";

describe("CacheMapsService object collision", () => {
    it("shares pending and completed definition reads and chunks distinct IDs at 256", async () => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(), CacheMapsService] });
        const service = TestBed.inject(CacheMapsService);
        const http = TestBed.inject(HttpTestingController);
        const ids = Array.from({ length: 257 }, (_, id) => id);
        const collision = { sizeX: 2, sizeY: 1, clipType: 1, solid: false, gateway: false };
        const matches = (request: { url: string }) => request.url.endsWith("/types/objects/collision");

        const all = firstValueFrom(service.objectCollision([...ids, 0]));
        const overlapping = firstValueFrom(service.objectCollision([0, 256]));
        const [first, second] = http.match(matches);
        expect(first.request.body).toEqual({ ids: ids.slice(0, 256) });
        expect(second.request.body).toEqual({ ids: [256] });
        first.flush([{ id: 0, status: "ok", collision }]);
        second.flush([{ id: 256, status: "not-found", collision: null }]);

        expect((await all).size).toBe(257);
        expect(await overlapping).toEqual(new Map([[0, collision], [256, null]]));
        expect(await firstValueFrom(service.objectCollision([256, 0]))).toEqual(await overlapping);
        http.verify();
    });
});
