import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { MatDialog } from "@angular/material/dialog";
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

    it("automatically previews the fixed detail ID and hides standalone lookup controls", () => {
        const fixture = TestBed.createComponent(SpritesManagerPageComponent);
        fixture.componentRef.setInput("spriteId", 12);
        fixture.detectChanges();
        const request = http.expectOne((req) => req.url === `${baseUrl}/12/image`);
        expect(request.request.params.get("frame")).toBe("0");
        request.flush(new Blob(["png"], { type: "image/png" }));
        fixture.detectChanges();
        expect(fixture.componentInstance.spriteUploadForm.id().value()).toBe(12);
        expect(fixture.nativeElement.textContent).not.toContain("Find by ID");
        expect(fixture.nativeElement.textContent).not.toContain("Search sprites");
        expect(fixture.nativeElement.querySelector("img").alt).toBe("Sprite #12, frame 0");
        expect(fixture.nativeElement.querySelector("input[readonly]")).not.toBeNull();
    });

    it("shows an offline preview error and retries the same sprite", () => {
        const fixture = TestBed.createComponent(SpritesManagerPageComponent);
        fixture.componentRef.setInput("spriteId", 12);
        fixture.detectChanges();
        http.expectOne((req) => req.url === `${baseUrl}/12/image`).error(new ProgressEvent("error"));
        fixture.detectChanges();
        expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain("Sprite request failed");
        expect(fixture.componentInstance.store.loading()).toBe(false);
        fixture.componentInstance.previewSpriteImage();
        http.expectOne((req) => req.url === `${baseUrl}/12/image`).flush(new Blob(["png"], { type: "image/png" }));
        fixture.detectChanges();
        expect(fixture.componentInstance.store.error()).toBeNull();
        expect(fixture.nativeElement.querySelector("img")).not.toBeNull();
    });

    it("refreshes the fixed sprite preview and emits saved metadata after confirmed replacement", async () => {
        const fixture = TestBed.createComponent(SpritesManagerPageComponent);
        fixture.componentRef.setInput("spriteId", 12);
        fixture.detectChanges();
        http.expectOne((req) => req.url === `${baseUrl}/12/image`).flush(new Blob(["old"], { type: "image/png" }));
        fixture.detectChanges();
        const page = fixture.componentInstance;
        const replaced = vi.fn();
        page.imageReplaced.subscribe(replaced);
        const file = new File(["png"], "replacement.png", { type: "image/png" });
        page.onFileSelected({ target: { files: [file] } } as unknown as Event);
        const saving = page.uploadSpriteImage();
        TestBed.inject(MatDialog).openDialogs[0].close(true);
        let upload!: import("@angular/common/http/testing").TestRequest;
        await vi.waitFor(() => {
            upload = http.expectOne((req) => req.url === `${baseUrl}/12/image` && req.method === "POST");
        });
        expect(upload.request.body.get("file")).toBe(file);
        const result = { operation: "sprite-image-replaced", type: "sprite", id: 12, width: 32, height: 32, frameCount: 1 };
        upload.flush(result);
        await saving;
        fixture.detectChanges();
        const preview = http.expectOne((req) => req.url === `${baseUrl}/12/image` && req.method === "GET");
        expect(preview.request.params.get("frame")).toBe("0");
        preview.flush(new Blob(["new"], { type: "image/png" }));
        fixture.detectChanges();
        expect(replaced).toHaveBeenCalledExactlyOnceWith(result);
        expect(revokeObjectURL).toHaveBeenCalledWith("blob:first");
    });

    it("submits sprite metadata lookup for the selected ID", async () => {
        const page = TestBed.createComponent(SpritesManagerPageComponent).componentInstance;
        page.spriteIdForm.id().value.set(12);
        const load = page.loadSprite();
        http.expectOne(`${baseUrl}/12`).flush({ id: 12, width: 32, height: 32, frameCount: 1 });
        await load;
        expect(page.store.spriteInfo()).toEqual({ id: 12, width: 32, height: 32, frameCount: 1 });
    });

    it("allows an empty sprite query but blocks invalid paging", async () => {
        const page = TestBed.createComponent(SpritesManagerPageComponent).componentInstance;
        page.spriteSearchForm.offset().value.set(-1);
        await page.searchSprites();
        http.expectNone(() => true);
        page.spriteSearchForm.offset().value.set(0);
        const search = page.searchSprites();
        const request = http.expectOne((req) => req.url === `${baseUrl}/search`);
        expect(request.request.params.get("query")).toBe("");
        expect(request.request.params.get("offset")).toBe("0");
        request.flush({ query: "", totalMatches: 0, offset: 0, limit: 10, items: [] });
        await search;
        expect(page.store.searchResult()?.items).toEqual([]);
    });

    it("validates preview coordinates before requesting the selected frame", async () => {
        const page = TestBed.createComponent(SpritesManagerPageComponent).componentInstance;
        page.spriteImageForm.frame().value.set(-1);
        await page.previewSpriteImage();
        http.expectNone(() => true);
        page.spriteImageForm.id().value.set(12);
        page.spriteImageForm.frame().value.set(3);
        const preview = page.previewSpriteImage();
        const request = http.expectOne((req) => req.url === `${baseUrl}/12/image`);
        expect(request.request.params.get("frame")).toBe("3");
        const blob = new Blob(["png"], { type: "image/png" });
        request.flush(blob);
        await preview;
        expect(page.store.imageBlob()).toBe(blob);
        expect(page.store.imageDescription()).toBe("Sprite #12, frame 3");
    });

    it("clears an empty file selection and rejects upload without a PNG", async () => {
        const page = TestBed.createComponent(SpritesManagerPageComponent).componentInstance;
        page.selectedFile.set(new File(["png"], "previous.png", { type: "image/png" }));
        page.onFileSelected({ target: { files: [] } } as unknown as Event);
        expect(page.selectedFile()).toBeNull();
        await page.uploadSpriteImage();
        http.expectNone(() => true);
        expect(page.store.error()).toBe("Select a PNG file before uploading.");
    });

    it.each([false, true])("requires confirmation before replacing a sprite PNG: %s", async (confirmed) => {
        const fixture = TestBed.createComponent(SpritesManagerPageComponent);
        const page = fixture.componentInstance;
        page.spriteUploadForm.id().value.set(12);
        const file = new File(["png"], "replacement.png", { type: "image/png" });
        page.onFileSelected({ target: { files: [file] } } as unknown as Event);
        const save = page.uploadSpriteImage();
        http.expectNone(() => true);
        const dialog = TestBed.inject(MatDialog).openDialogs[0];
        expect(dialog.componentInstance.description).toBe("Replace sprite #12 with replacement.png?");
        dialog.close(confirmed);
        if (confirmed) {
            await vi.waitFor(() => expect(page.store.loading()).toBe(true));
            const request = http.expectOne(`${baseUrl}/12/image`);
            expect((request.request.body as FormData).get("file")).toBe(file);
            request.flush({ operation: "update", type: "sprites", id: 12, width: 32, height: 32, frameCount: 1 });
        }
        await save;
        expect(page.store.mutationResult()?.id ?? null).toBe(confirmed ? 12 : null);
    });

    it("revokes the old preview on replacement and the active preview on destruction", async () => {
        const fixture = TestBed.createComponent(SpritesManagerPageComponent);
        fixture.detectChanges();
        const firstBlob = new Blob(["first"], { type: "image/png" });
        const first = fixture.componentInstance.store.previewSpriteImage({ id: 12, frame: 0 });
        http.expectOne((req) => req.url === `${baseUrl}/12/image`).flush(firstBlob);
        await first;
        fixture.detectChanges();
        expect(createObjectURL).toHaveBeenCalledWith(firstBlob);
        expect(fixture.componentInstance.imageUrl()).toBe("blob:first");
        expect(revokeObjectURL).not.toHaveBeenCalled();

        const secondBlob = new Blob(["second"], { type: "image/png" });
        const second = fixture.componentInstance.store.previewSpriteImage({ id: 12, frame: 1 });
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
        const pending = fixture.componentInstance.store.previewSpriteImage({ id: 12, frame: 0 });
        const request = http.expectOne((req) => req.url === `${baseUrl}/12/image`);
        fixture.destroy();
        expect(request.cancelled).toBe(true);
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
