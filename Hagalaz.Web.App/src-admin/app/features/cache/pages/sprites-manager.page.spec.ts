import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../environments/environment";
import { CacheSpritesStore } from "../stores/cache-sprites.store";
import { SpritesManagerPageComponent } from "./sprites-manager.page";

const baseUrl = `${environment.cacheApiUrl}types/sprites`;

describe("SpritesManagerPage preview lifecycle", () => {
    let http: HttpTestingController;
    let createObjectURL: ReturnType<typeof vi.fn<typeof URL.createObjectURL>>;
    let revokeObjectURL: ReturnType<typeof vi.fn<typeof URL.revokeObjectURL>>;

    beforeEach(() => {
        createObjectURL = vi.fn<typeof URL.createObjectURL>().mockReturnValueOnce("blob:first").mockReturnValueOnce("blob:second");
        revokeObjectURL = vi.fn<typeof URL.revokeObjectURL>();
        vi.stubGlobal(
            "URL",
            class extends URL {
                static override createObjectURL = createObjectURL;
                static override revokeObjectURL = revokeObjectURL;
            }
        );
        TestBed.configureTestingModule({
            imports: [SpritesManagerPageComponent],
            providers: [provideHttpClient(withXhr()), provideHttpClientTesting(), CacheSpritesStore],
        });
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        TestBed.resetTestingModule();
        vi.unstubAllGlobals();
        http.verify();
    });

    it("revokes the old preview on replacement and the active preview on destruction", async () => {
        const fixture = TestBed.createComponent(SpritesManagerPageComponent);
        fixture.detectChanges();
        const firstBlob = new Blob(["first"], { type: "image/png" });
        const first = fixture.componentInstance.store.previewSpriteImage(12, 0);
        http.expectOne((req) => req.url === `${baseUrl}/12/image`).flush(firstBlob);
        await first;
        fixture.detectChanges();
        expect(createObjectURL).toHaveBeenCalledWith(firstBlob);
        expect(fixture.componentInstance.imageUrl()).toBe("blob:first");
        expect(revokeObjectURL).not.toHaveBeenCalled();

        const secondBlob = new Blob(["second"], { type: "image/png" });
        const second = fixture.componentInstance.store.previewSpriteImage(12, 1);
        http.expectOne((req) => req.url === `${baseUrl}/12/image`).flush(secondBlob);
        await second;
        fixture.detectChanges();
        expect(createObjectURL).toHaveBeenLastCalledWith(secondBlob);
        expect(fixture.componentInstance.imageUrl()).toBe("blob:second");
        expect(revokeObjectURL).toHaveBeenCalledExactlyOnceWith("blob:first");

        fixture.destroy();
        expect(revokeObjectURL.mock.calls).toEqual([["blob:first"], ["blob:second"]]);
    });

    it("does not create a preview URL for a response received after page destruction", async () => {
        const fixture = TestBed.createComponent(SpritesManagerPageComponent);
        fixture.detectChanges();
        const pending = fixture.componentInstance.store.previewSpriteImage(12, 0);
        const request = http.expectOne((req) => req.url === `${baseUrl}/12/image`);
        fixture.destroy();
        request.flush(new Blob(["late"], { type: "image/png" }));
        await pending;
        expect(createObjectURL).not.toHaveBeenCalled();
        expect(revokeObjectURL).not.toHaveBeenCalled();
    });

    it("provides independent stores for separate page components", async () => {
        const first = TestBed.createComponent(SpritesManagerPageComponent);
        const second = TestBed.createComponent(SpritesManagerPageComponent);
        first.detectChanges();
        second.detectChanges();
        const firstStore = first.componentInstance.store;
        const secondStore = second.componentInstance.store;
        expect(firstStore).not.toBe(secondStore);
        expect(firstStore).not.toBe(TestBed.inject(CacheSpritesStore));

        const pending = firstStore.loadSprite(12);
        http.expectOne(`${baseUrl}/12`).flush({ id: 12 });
        await pending;
        expect(firstStore.spriteInfo()).toEqual({ id: 12 });
        expect(secondStore.spriteInfo()).toBeNull();
        expect(secondStore.imageBlob()).toBeNull();
        expect(secondStore.loading()).toBe(false);
        expect(TestBed.inject(CacheSpritesStore).spriteInfo()).toBeNull();
    });
});
