import { provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../environments/environment";
import { CacheTextureThumbnailComponent } from "./cache-texture-thumbnail.component";

describe("CacheTextureThumbnailComponent", () => {
    let http: HttpTestingController;
    beforeEach(() => {
        vi.stubGlobal("URL", class extends URL {
            static override createObjectURL = vi.fn(() => "blob:texture-preview");
            static override revokeObjectURL = vi.fn();
        });
        TestBed.configureTestingModule({ imports: [CacheTextureThumbnailComponent], providers: [provideHttpClient(), provideHttpClientTesting()] });
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => { http.verify(); vi.unstubAllGlobals(); });

    it("renders a missing texture marker without throwing a resource error", async () => {
        const fixture = TestBed.createComponent(CacheTextureThumbnailComponent);
        fixture.componentRef.setInput("id", 8); fixture.detectChanges(); TestBed.tick();
        http.expectOne(`${environment.cacheApiUrl}textures/8/image`).flush(new Blob(), {status: 404, statusText: "Not Found"});
        await vi.waitFor(() => { fixture.detectChanges(); TestBed.tick(); expect(fixture.nativeElement.textContent).toContain("Image unavailable"); });
        expect(fixture.nativeElement.querySelector("img")).toBeNull();
    });

    it("releases the displayed object URL when destroyed", async () => {
        const fixture = TestBed.createComponent(CacheTextureThumbnailComponent);
        fixture.componentRef.setInput("id", 8); fixture.detectChanges(); TestBed.tick();
        http.expectOne(`${environment.cacheApiUrl}textures/8/image`).flush(new Blob());
        fixture.detectChanges(); TestBed.tick();
        await vi.waitFor(() => { fixture.detectChanges(); TestBed.tick(); expect(fixture.nativeElement.querySelector("img")?.getAttribute("src")).toBe("blob:texture-preview"); });
        fixture.destroy();
        expect(URL.revokeObjectURL).toHaveBeenCalledExactlyOnceWith("blob:texture-preview");
    });
});
