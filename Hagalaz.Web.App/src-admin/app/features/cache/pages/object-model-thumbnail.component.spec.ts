import { provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../environments/environment";
import { ObjectModelThumbnailComponent } from "./object-model-thumbnail.component";

const imageUrl = (id: number) => `${environment.cacheApiUrl}types/objects/${id}/image`;

describe("ObjectModelThumbnailComponent", () => {
    let http: HttpTestingController;
    let intersect: (entries: { isIntersecting: boolean }[]) => void;
    let urls: number;

    beforeEach(() => {
        urls = 0;
        vi.stubGlobal("IntersectionObserver", class {
            constructor(callback: typeof intersect) { intersect = callback; }
            observe = vi.fn();
            disconnect = vi.fn();
        });
        vi.stubGlobal("devicePixelRatio", 2);
        vi.stubGlobal("URL", class extends URL {
            static override createObjectURL = vi.fn(() => `blob:object-${++urls}`);
            static override revokeObjectURL = vi.fn();
        });
        TestBed.configureTestingModule({ imports: [ObjectModelThumbnailComponent], providers: [provideHttpClient(), provideHttpClientTesting()] });
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => { http.verify(); vi.unstubAllGlobals(); });

    function create(id: number, shape: number | null, visible = true) {
        const fixture = TestBed.createComponent(ObjectModelThumbnailComponent);
        fixture.componentRef.setInput("id", id);
        fixture.componentRef.setInput("shape", shape);
        fixture.componentRef.setInput("name", "Crate");
        fixture.detectChanges();
        TestBed.tick();
        if (visible) { intersect([{ isIntersecting: true }]); TestBed.tick(); }
        return fixture;
    }
    const element = (fixture: { nativeElement: unknown }) => fixture.nativeElement as HTMLElement;

    it("waits until visible, then requests the shape at device pixels and shows the image", () => {
        const fixture = create(1, 10, false);
        http.expectNone(() => true);
        intersect([{ isIntersecting: true }]);
        TestBed.tick();
        const request = http.expectOne(candidate => candidate.url === imageUrl(1));
        expect(request.request.params.keys().map(key => [key, request.request.params.get(key)])).toEqual([["width", "192"], ["height", "128"], ["shape", "10"]]);
        request.flush(new Blob(["png"]));
        fixture.detectChanges();
        const image = element(fixture).querySelector("img");
        expect(image?.getAttribute("src")).toBe("blob:object-1");
        expect(image?.alt).toBe("Crate");
    });

    it("keeps the placeholder for objects without a shape and for failed renders", () => {
        const none = create(2, null);
        http.expectNone(() => true);
        expect(element(none).querySelector("mat-icon")).not.toBeNull();
        const failed = create(3, 10);
        http.expectOne(candidate => candidate.url === imageUrl(3)).flush(new Blob(), { status: 422, statusText: "Unsupported" });
        failed.detectChanges();
        expect(element(failed).querySelector("img")).toBeNull();
        expect(element(failed).querySelector("mat-icon")).not.toBeNull();
    });

    it("replaces the image for a new object and releases object URLs", () => {
        const fixture = create(4, 10);
        http.expectOne(candidate => candidate.url === imageUrl(4)).flush(new Blob(["png"]));
        fixture.componentRef.setInput("id", 5);
        TestBed.tick();
        expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:object-1");
        http.expectOne(candidate => candidate.url === imageUrl(5)).flush(new Blob(["png"]));
        fixture.destroy();
        expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:object-2");
    });
});
