import { provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../environments/environment";
import { ItemSpriteThumbnailComponent } from "./item-sprite-thumbnail.component";

const itemImageUrl = (id: number) => `${environment.cacheApiUrl}types/items/${id}/image`;

describe("ItemSpriteThumbnailComponent", () => {
    let http: HttpTestingController;
    let objectUrls: number;

    beforeEach(() => {
        objectUrls = 0;
        vi.stubGlobal("devicePixelRatio", 2);
        vi.stubGlobal(
            "URL",
            class extends URL {
                static override createObjectURL = vi.fn(() => `blob:item-preview-${++objectUrls}`);
                static override revokeObjectURL = vi.fn();
            }
        );
        TestBed.configureTestingModule({
            imports: [ItemSpriteThumbnailComponent],
            providers: [provideHttpClient(), provideHttpClientTesting()],
        });
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        http.verify();
        vi.unstubAllGlobals();
    });

    function create(id = 42) {
        const fixture = TestBed.createComponent(ItemSpriteThumbnailComponent);
        fixture.componentRef.setInput("id", id);
        fixture.componentRef.setInput("name", "Initial sword");
        fixture.detectChanges();
        TestBed.tick();
        return fixture;
    }

    function expectImage(id = 42): TestRequest {
        return http.expectOne((request) => request.url === itemImageUrl(id));
    }

    it("requests the rendered icon at the displayed device-pixel size and releases its object URL", () => {
        const fixture = create();
        const request = expectImage();
        expect(request.request.responseType).toBe("blob");
        expect(request.request.params.get("width")).toBe("88");
        expect(request.request.params.get("height")).toBe("88");
        expect(request.request.params.has("quantity")).toBe(false);
        request.flush(new Blob(["png"], { type: "image/png" }));
        fixture.detectChanges();

        const image = fixture.nativeElement.querySelector("img") as HTMLImageElement;
        expect(image.getAttribute("src")).toBe("blob:item-preview-1");
        expect(image.getAttribute("alt")).toBe("Item sprite for Initial sword");
        expect(image.getAttribute("width")).toBe("44");

        fixture.destroy();
        expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:item-preview-1");
    });

    it("forwards rendering options and clamps the size to the server limit", () => {
        const fixture = create();
        expectImage().flush(new Blob());
        fixture.componentRef.setInput("size", 400);
        fixture.componentRef.setInput("quantity", 25000);
        fixture.componentRef.setInput("outline", 2);
        fixture.componentRef.setInput("shadow", "none");
        fixture.detectChanges();
        TestBed.tick();

        const params = expectImage().request.params;
        expect(params.get("width")).toBe("512");
        expect(params.get("quantity")).toBe("25000");
        expect(params.get("outline")).toBe("2");
        expect(params.get("shadow")).toBe("none");
    });

    it("refetches for a new revision and keeps the previous icon until the replacement arrives", () => {
        const fixture = create();
        expectImage().flush(new Blob());
        fixture.detectChanges();
        fixture.componentRef.setInput("revision", { saved: true });
        fixture.detectChanges();
        TestBed.tick();

        const request = expectImage();
        expect(fixture.nativeElement.querySelector("img")?.getAttribute("src")).toBe("blob:item-preview-1");
        expect(URL.revokeObjectURL).not.toHaveBeenCalled();
        request.flush(new Blob());
        fixture.detectChanges();
        expect(fixture.nativeElement.querySelector("img")?.getAttribute("src")).toBe("blob:item-preview-2");
        expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:item-preview-1");
    });

    it("clears the previous item's icon while another item loads", () => {
        const fixture = create();
        expectImage().flush(new Blob());
        fixture.detectChanges();
        fixture.componentRef.setInput("id", 43);
        fixture.detectChanges();
        TestBed.tick();
        fixture.detectChanges();

        expect(fixture.nativeElement.querySelector("img")).toBeNull();
        expect(fixture.nativeElement.querySelector('[role="status"]')).not.toBeNull();
        expectImage(43).flush(new Blob());
    });

    it("shows an unavailable marker when the image request fails", () => {
        const fixture = create();
        expectImage().flush(new Blob(), { status: 404, statusText: "Not Found" });
        fixture.detectChanges();

        const placeholder = fixture.nativeElement.querySelector('[role="img"]') as HTMLElement;
        expect(placeholder?.getAttribute("aria-label")).toBe("Item sprite unavailable for item #42");
        expect(fixture.nativeElement.querySelector("img")).toBeNull();
    });
});
