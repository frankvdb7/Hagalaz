import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { MatDialog } from "@angular/material/dialog";
import { By } from "@angular/platform-browser";
import { provideRouter } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../../environments/environment";
import { SpriteInspectorComponent } from "../sprite-inspector.component";
import { SpriteDefinitionsDetailPageComponent } from "./sprites-detail.page";

const baseUrl = `${environment.cacheApiUrl}types/sprites`;
const png = () => new Blob(["png"], { type: "image/png" });
let imageDecoders: { naturalWidth: number; naturalHeight: number; onload: (() => void) | null; onerror: (() => void) | null; src: string }[];

describe("Sprite Studio", () => {
    let http: HttpTestingController;
    let created: string[];
    let revoked: string[];
    beforeEach(() => {
        Object.defineProperty(HTMLElement.prototype, "scrollTo", { configurable: true, value: vi.fn() });
        created = [];
        revoked = [];
        imageDecoders = [];
        vi.stubGlobal(
            "URL",
            class extends URL {
                static override createObjectURL = vi.fn(() => {
                    const url = `blob:preview-${created.length}`;
                    created.push(url);
                    return url;
                });
                static override revokeObjectURL = vi.fn((url: string) => revoked.push(url));
            }
        );
        vi.stubGlobal(
            "Image",
            class {
                naturalWidth = 64;
                naturalHeight = 48;
                onload: (() => void) | null = null;
                onerror: (() => void) | null = null;
                src = "";
                constructor() {
                    imageDecoders.push(this);
                }
            }
        );
        TestBed.configureTestingModule({
            providers: [
                provideRouter([{ path: "cache/types/sprites/:id", component: SpriteDefinitionsDetailPageComponent }]),
                provideHttpClient(withXhr()),
                provideHttpClientTesting(),
            ],
        });
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => {
        TestBed.inject(MatDialog).closeAll();
        TestBed.resetTestingModule();
        http.verify({ ignoreCancelled: true });
        Reflect.deleteProperty(HTMLElement.prototype, "scrollTo");
        vi.unstubAllGlobals();
    });
    function drainImages() {
        TestBed.tick();
        const requests = http.match((r) => r.url.endsWith("/image") && r.method === "GET");
        for (const request of requests) if (!request.cancelled) request.flush(png());
        TestBed.tick();
        return requests;
    }
    async function open(frameCount = 4) {
        const opening = RouterTestingHarness.create("/cache/types/sprites/7");
        await vi.waitFor(() => http.expectOne(`${baseUrl}/7`).flush({ id: 7, width: 32, height: 16, frameCount }));
        if (frameCount) await vi.waitFor(() => expect(drainImages().length).toBeGreaterThan(0));
        const harness = await opening;
        drainImages();
        const page = harness.fixture.debugElement.query(By.directive(SpriteDefinitionsDetailPageComponent))
            .componentInstance as SpriteDefinitionsDetailPageComponent;
        const editor = harness.fixture.debugElement.query(By.directive(SpriteInspectorComponent)).componentInstance as SpriteInspectorComponent;
        return { harness, page, editor, element: harness.routeNativeElement as HTMLElement };
    }
    async function chooseFile(editor: SpriteInspectorComponent, type = "image/png") {
        const file = new File(["png"], "replacement.png", { type });
        editor.onFileSelected({ target: { files: [file], value: "" } } as unknown as Event);
        TestBed.tick();
        return file;
    }

    it("renders real metadata in the dedicated route workspace", async () => {
        const { element, page } = await open();
        expect(element.querySelector("cache-definition-detail")).toBeNull();
        expect(element.querySelector(".workspace")).not.toBeNull();
        expect(element.querySelector(".properties")?.textContent).toContain("32 × 16 px");
        expect(page.metadata()?.frameCount).toBe(4);
        expect(element.querySelector<HTMLImageElement>(".sprite-image")?.src).toMatch(/^blob:/);
        expect(element.querySelector('nav[aria-label="Breadcrumb"]')).toBeNull();
    });

    it.each([0, 1])("handles a sprite with %s frames without requesting nonexistent frames", async (count) => {
        const { element, page } = await open(count);
        expect(page.selectedFrame()).toBe(count ? 0 : null);
        expect(element.textContent).toContain(count ? "Frame count" : "No frames to preview");
        page.selectFrame(-1);
        page.selectFrame(count);
        expect(page.selectedFrame()).toBe(count ? 0 : null);
        http.expectNone(() => true);
    });

    it("navigates frames by keyboard and ignores out-of-range selection", async () => {
        const { element, page } = await open();
        const list = element.querySelector('[role="listbox"]') as HTMLElement;
        list.focus();
        list.dispatchEvent(new KeyboardEvent("keydown", { key: "ArrowDown", bubbles: true }));
        drainImages();
        expect(page.selectedFrame()).toBe(1);
        expect(list.getAttribute("aria-activedescendant")).toBe("sprite-frame-1");
        expect(document.activeElement).toBe(list);
        expect(page.images.imageFrame()).toBe(1);
        page.selectFrame(4);
        expect(page.selectedFrame()).toBe(1);
        list.dispatchEvent(new KeyboardEvent("keydown", { key: "End", bubbles: true }));
        drainImages();
        expect(page.selectedFrame()).toBe(3);
        list.dispatchEvent(new KeyboardEvent("keydown", { key: "Home", bubbles: true }));
        drainImages();
        expect(page.selectedFrame()).toBe(0);
    });

    it("requests a bounded visible window of real thumbnails for long lists", async () => {
        const { element, page } = await open(500);
        const thumbnails = element.querySelectorAll("cache-sprite-frame-thumbnail");
        expect(thumbnails.length).toBeGreaterThan(0);
        expect(thumbnails.length).toBeLessThan(20);
        expect(thumbnails[0].querySelector("img")).not.toBeNull();
        page.selectFrame(499);
        drainImages();
        expect(page.images.imageFrame()).toBe(499);
    });

    it("changes zoom and background and downloads the selected PNG", async () => {
        const { element, page } = await open();
        page.changeZoom(2);
        page.background.set("solid");
        TestBed.tick();
        const image = element.querySelector(".sprite-image") as HTMLImageElement;
        expect(image.style.width).toBe("64px");
        expect(element.querySelector(".image-viewport")?.classList.contains("checkerboard")).toBe(false);
        const click = vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(function (this: HTMLAnchorElement) {
            expect(this.download).toBe("sprite-7-frame-0.png");
            expect(this.href).toBe(page.currentImageUrl());
        });
        page.downloadFrame();
        expect(click).toHaveBeenCalledOnce();
        page.fit.set(true);
        TestBed.tick();
        expect(image.classList.contains("fit-image")).toBe(true);
        click.mockRestore();
    });

    it("rejects invalid file types and undecodable images before confirmation", async () => {
        const { editor } = await open();
        await chooseFile(editor, "image/jpeg");
        expect(editor.fileError()).toContain("Choose a PNG");
        editor.replaceSprite();
        expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(0);
        await chooseFile(editor);
        imageDecoders.at(-1)?.onerror?.();
        expect(editor.fileError()).toContain("could not be decoded");
        expect(editor.candidate()).toBeNull();
        editor.replaceSprite();
        http.expectNone((r) => r.method === "POST");
    });

    it("shows a before/after preview and sends no mutation when confirmation is canceled", async () => {
        const { page, editor } = await open();
        await chooseFile(editor);
        imageDecoders.at(-1)?.onload?.();
        TestBed.tick();
        expect(editor.candidate()?.width).toBe(64);
        page.inspectorTab.set(1);
        TestBed.tick();
        expect(document.querySelectorAll(".comparison img")).toHaveLength(2);
        editor.replaceSprite();
        const dialog = TestBed.inject(MatDialog).openDialogs[0];
        expect(dialog.componentInstance.description).toContain("entire sprite #7");
        dialog.close(false);
        await vi.waitFor(() => expect(editor.confirming()).toBe(false));
        expect(editor.candidate()).not.toBeNull();
        http.expectNone((r) => r.method === "POST");
    });

    it("replaces the whole sprite, refreshes metadata once, and clamps the selected frame", async () => {
        const { page, editor } = await open();
        page.selectFrame(3);
        drainImages();
        const file = await chooseFile(editor);
        imageDecoders.at(-1)?.onload?.();
        editor.replaceSprite();
        TestBed.inject(MatDialog).openDialogs[0].close(true);
        await vi.waitFor(() => expect(page.images.saving()).toBe(true));
        const upload = http.expectOne((r) => r.url === `${baseUrl}/7/image` && r.method === "POST");
        expect(upload.request.body.get("file")).toBe(file);
        upload.flush({ operation: "sprite-image-replaced", type: "sprite", id: 7, width: 64, height: 48, frameCount: 1 });
        TestBed.tick();
        http.expectOne(`${baseUrl}/7`).flush({ id: 7, width: 64, height: 48, frameCount: 1 });
        await vi.waitFor(() => {
            drainImages();
            expect(page.selectedFrame()).toBe(0);
        });
        expect(page.images.imageFrame()).toBe(0);
        expect(page.metadata()?.width).toBe(64);
        expect(editor.confirmation()?.frameCount).toBe(1);
        expect(editor.selectedFile()).toBeNull();
        TestBed.tick();
        http.expectNone(() => true);
    });

    it("keeps the candidate and original preview after a failed replacement and allows retry", async () => {
        const { page, editor } = await open();
        const original = page.currentImageUrl();
        await chooseFile(editor);
        imageDecoders.at(-1)?.onload?.();
        editor.replaceSprite();
        TestBed.inject(MatDialog).openDialogs[0].close(true);
        await vi.waitFor(() => expect(page.images.saving()).toBe(true));
        http.expectOne((r) => r.method === "POST").flush({ detail: "Write failed" }, { status: 500, statusText: "Error" });
        expect(editor.confirmation()).toBeNull();
        expect(page.images.mutationError()).toContain("Write failed");
        expect(editor.candidate()).not.toBeNull();
        expect(page.currentImageUrl()).toBe(original);
        editor.replaceSprite();
        expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(1);
        TestBed.inject(MatDialog).openDialogs[0].close(false);
    });

    it("cancels old frame requests and hides old images when navigating to another sprite", async () => {
        const { page, harness } = await open(500);
        page.selectFrame(20);
        TestBed.tick();
        const requests = http.match((r) => r.url.endsWith("/image"));
        expect(requests.length).toBeGreaterThan(0);
        const next = harness.navigateByUrl("/cache/types/sprites/8");
        await vi.waitFor(() => http.expectOne(`${baseUrl}/8`).flush({ id: 8, width: 16, height: 16, frameCount: 1 }));
        expect(requests.every((r) => r.cancelled)).toBe(true);
        expect(page.currentImageUrl()).toBeNull();
        await vi.waitFor(() => expect(drainImages().length).toBeGreaterThan(0));
        await next;
        drainImages();
        expect(page.images.imageId()).toBe(8);
        expect(page.metadata()?.id).toBe(8);
    });

    it("revokes all preview and candidate URLs when superseded or destroyed", async () => {
        const { page, editor } = await open();
        const old = page.currentImageUrl();
        page.selectFrame(2);
        drainImages();
        expect(revoked).toContain(old);
        await chooseFile(editor);
        const candidateUrl = imageDecoders.at(-1)?.src;
        await chooseFile(editor);
        expect(revoked).toContain(candidateUrl);
        TestBed.resetTestingModule();
        expect(new Set(revoked)).toEqual(new Set(created));
    });
    it("rejects invalid route IDs and supports retrying failed metadata", async () => {
        const harness = await RouterTestingHarness.create("/cache/types/sprites/invalid");
        expect(harness.routeNativeElement?.textContent).toContain("valid sprite ID");
        http.expectNone(() => true);
        const navigating = harness.navigateByUrl("/cache/types/sprites/7");
        await vi.waitFor(() => http.expectOne(`${baseUrl}/7`).flush({ detail: "Sprite unavailable" }, { status: 503, statusText: "Unavailable" }));
        await navigating;
        await vi.waitFor(() => expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain("Sprite unavailable"));
        const page = harness.fixture.debugElement.query(By.directive(SpriteDefinitionsDetailPageComponent))
            .componentInstance as SpriteDefinitionsDetailPageComponent;
        page.reload();
        TestBed.tick();
        http.expectOne(`${baseUrl}/7`).flush({ id: 7, width: 32, height: 16, frameCount: 0 });
        await vi.waitFor(() => expect(page.metadata()?.id).toBe(7));
        expect(harness.routeNativeElement?.textContent).toContain("No frames to preview");
    });

    it("preserves a correctly labeled previous frame on preview failure and retries the selected frame", async () => {
        const { page, element } = await open(500);
        const previous = page.currentImageUrl();
        page.selectFrame(20);
        TestBed.tick();
        const requests = http.match((r) => r.url.endsWith("/image"));
        for (const request of requests) if (!request.cancelled) request.error(new ProgressEvent("error"));
        TestBed.tick();
        expect(page.currentImageUrl()).toBe(previous);
        expect(element.querySelector(".sprite-image")?.getAttribute("alt")).toBe("Sprite #7, frame 0");
        expect(page.canDownload()).toBe(false);
        expect(element.querySelector('[role="alert"]')?.textContent).toContain(`${baseUrl}/7/image`);
        page.retryPreview();
        drainImages();
        expect(page.images.imageFrame()).toBe(20);
        expect(page.canDownload()).toBe(true);
    });

    it("clears the preview when refreshed metadata has zero frames", async () => {
        const { page } = await open();
        page.reload();
        TestBed.tick();
        http.expectOne(`${baseUrl}/7`).flush({ id: 7, width: 0, height: 0, frameCount: 0 });
        await vi.waitFor(() => {
            TestBed.tick();
            expect(page.selectedFrame()).toBeNull();
            expect(page.currentImageUrl()).toBeNull();
        });
        http.expectNone((r) => r.url.endsWith("/image"));
    });

    it("discards an obsolete image decoder and validates usable PNG dimensions", async () => {
        const { editor } = await open();
        await chooseFile(editor);
        const obsolete = imageDecoders.at(-1);
        await chooseFile(editor);
        expect(obsolete?.onload).toBeNull();
        const decoder = imageDecoders.at(-1);
        if (!decoder) throw new Error("Missing image decoder");
        decoder.naturalWidth = 0;
        decoder.onload?.();
        expect(editor.candidate()).toBeNull();
        expect(editor.fileError()).toContain("no usable image dimensions");
        editor.onFileSelected({ target: { files: [], value: "" } } as unknown as Event);
        TestBed.tick();
        expect(editor.selectedFile()).toBeNull();
        expect(editor.fileError()).toBeNull();
    });

    it("does not download while the selected frame is pending and bounds zoom controls", async () => {
        const { page } = await open(500);
        page.changeZoom(100);
        expect(page.zoom()).toBe(1600);
        expect(page.zoomInDisabled()).toBe(true);
        page.changeZoom(0.001);
        expect(page.zoom()).toBe(25);
        expect(page.zoomOutDisabled()).toBe(true);
        page.selectFrame(20);
        TestBed.tick();
        const click = vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(() => {});
        page.downloadFrame();
        expect(click).not.toHaveBeenCalled();
        drainImages();
        click.mockRestore();
    });
    it("does not show a late mutation failure as an error for a different sprite", async () => {
        const { page, editor, harness } = await open();
        await chooseFile(editor);
        imageDecoders.at(-1)?.onload?.();
        editor.replaceSprite();
        TestBed.inject(MatDialog).openDialogs[0].close(true);
        await vi.waitFor(() => expect(page.images.saving()).toBe(true));
        const upload = http.expectOne((r) => r.method === "POST");
        const navigating = harness.navigateByUrl("/cache/types/sprites/8");
        await vi.waitFor(() => http.expectOne(`${baseUrl}/8`).flush({ id: 8, width: 16, height: 16, frameCount: 0 }));
        await navigating;
        upload.flush({ detail: "Old sprite could not be replaced" }, { status: 500, statusText: "Error" });
        TestBed.tick();
        expect(editor.mutationError()).toBeNull();
        expect(editor.confirmation()).toBeNull();
        expect(editor.selectedFile()).toBeNull();
        expect(page.metadata()?.id).toBe(8);
    });
});
