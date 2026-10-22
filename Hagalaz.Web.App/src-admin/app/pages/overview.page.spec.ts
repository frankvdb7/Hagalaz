import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { ApplicationRef } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { provideRouter } from "@angular/router";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { environment } from "../../environments/environment";
import { ArchiveCountCardComponent } from "./archive-count-card";

describe("OverviewPage cache counts", () => {
    let http: HttpTestingController;
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [ArchiveCountCardComponent],
            providers: [provideRouter([]), provideHttpClient(withXhr()), provideHttpClientTesting()],
        });
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => http.verify());

    it("loads genuine archive counts on demand and preserves a zero total", async () => {
        const fixture = TestBed.createComponent(ArchiveCountCardComponent);
        const page = fixture.componentInstance;
        expect(page.definitionCount()).toBeNull();
        http.expectNone(() => true);
        page.cache.loadArchiveSizes();
        TestBed.tick();
        fixture.detectChanges();
        expect(page.loadLabel()).toBe("Loading…");
        expect(fixture.nativeElement.querySelector("button").disabled).toBe(true);
        http.expectOne(`${environment.cacheApiUrl}types/archive-sizes`).flush({ items: 2, npcs: 3, objects: 0 });
        await TestBed.inject(ApplicationRef).whenStable();
        expect(page.definitionCount()).toBe("5");
        page.cache.loadArchiveSizes();
        TestBed.tick();
        http.expectOne(`${environment.cacheApiUrl}types/archive-sizes`).flush({ items: 0, npcs: 0, objects: 0 });
        await TestBed.inject(ApplicationRef).whenStable();
        fixture.detectChanges();
        expect(page.definitionCount()).toBe("0");
        expect(fixture.nativeElement.textContent).toContain("Refresh counts");
    });

    it("renders an archive failure and allows an explicit retry", async () => {
        const fixture = TestBed.createComponent(ArchiveCountCardComponent);
        const page = fixture.componentInstance;
        page.cache.loadArchiveSizes();
        TestBed.tick();
        http.expectOne(`${environment.cacheApiUrl}types/archive-sizes`).flush({ detail: "Cache unavailable" }, { status: 503, statusText: "Unavailable" });
        await TestBed.inject(ApplicationRef).whenStable();
        fixture.detectChanges();
        expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toBe("Cache unavailable");
        expect(page.definitionCount()).toBeNull();
        page.cache.loadArchiveSizes();
        TestBed.tick();
        http.expectOne(`${environment.cacheApiUrl}types/archive-sizes`).flush({ items: 4 });
        await TestBed.inject(ApplicationRef).whenStable();
        fixture.detectChanges();
        expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
        expect(page.definitionCount()).toBe("4");
    });
});
