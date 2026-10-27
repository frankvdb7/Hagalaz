import { provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../environments/environment";
import { ItemSpriteThumbnailComponent } from "./item-sprite-thumbnail.component";

const itemImageUrl = `${environment.cacheApiUrl}types/items/42/image`;

describe("ItemSpriteThumbnailComponent", () => {
    let http: HttpTestingController;

    beforeEach(() => {
        vi.stubGlobal(
            "URL",
            class extends URL {
                static override createObjectURL = vi.fn(() => "blob:item-preview");
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

    it("fetches the item image as a blob and releases its object URL", () => {
        const fixture = TestBed.createComponent(ItemSpriteThumbnailComponent);
        fixture.componentRef.setInput("id", 42);
        fixture.componentRef.setInput("name", "Initial sword");
        fixture.detectChanges();
        TestBed.tick();

        const request = http.expectOne(itemImageUrl);
        expect(request.request.responseType).toBe("blob");
        request.flush(new Blob(["gif"], { type: "image/gif" }));
        fixture.detectChanges();

        const image = fixture.nativeElement.querySelector("img") as HTMLImageElement;
        expect(image.getAttribute("src")).toBe("blob:item-preview");
        expect(image.getAttribute("alt")).toBe("Item sprite for Initial sword");

        fixture.destroy();
        expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:item-preview");
    });

    it("shows an unavailable marker when the image request fails", () => {
        const fixture = TestBed.createComponent(ItemSpriteThumbnailComponent);
        fixture.componentRef.setInput("id", 42);
        fixture.detectChanges();
        TestBed.tick();

        http.expectOne(itemImageUrl).flush(new Blob(), { status: 404, statusText: "Not Found" });
        fixture.detectChanges();

        const placeholder = fixture.nativeElement.querySelector('[role="img"]') as HTMLElement;
        expect(placeholder?.getAttribute("aria-label")).toBe("Item sprite unavailable for item #42");
        expect(fixture.nativeElement.querySelector("img")).toBeNull();
    });
});
