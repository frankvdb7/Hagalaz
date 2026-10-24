import { TestbedHarnessEnvironment } from "@angular/cdk/testing/testbed";
import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from "@angular/common/http/testing";
import { ApplicationRef, Component } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { MatPaginatorHarness } from "@angular/material/paginator/testing";
import { provideRouter, Router } from "@angular/router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../environments/environment";
import { TypesManagerPageComponent } from "./types-manager.page";

@Component({ template: "" })
class DetailBoundary {}
const baseUrl = `${environment.cacheApiUrl}types/`;
const stable = () => TestBed.inject(ApplicationRef).whenStable();

describe("TypesManagerPage per-type navigation", () => {
    let http: HttpTestingController;
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [TypesManagerPageComponent],
            providers: [
                provideRouter([{ path: "cache/types/:kind/:id", component: DetailBoundary }]),
                provideHttpClient(withXhr()),
                provideHttpClientTesting(),
            ],
        });
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => http.verify());
    async function createPage() {
        const fixture = TestBed.createComponent(TypesManagerPageComponent);
        fixture.detectChanges();
        http.expectOne(`${baseUrl}archive-sizes`).flush({
            items: 23,
            objects: 30,
            npcs: 40,
            sprites: 20,
            varpBits: 15,
            configDefinitions: 12,
            clientMapDefinitions: 18,
        });
        TestBed.tick();
        let initial!: TestRequest;
        await vi.waitFor(() => {
            initial = http.expectOne((r) => r.url === `${baseUrl}items/range`);
        });
        expect(initial.request.params.get("startId")).toBe("0");
        expect(initial.request.params.get("endIdExclusive")).toBe("10");
        initial.flush([{ id: 0, name: "Initial sword" }]);
        await stable();
        return fixture;
    }
    it("automatically loads the first ten definitions and keeps tabs inside the search card", async () => {
        const fixture = await createPage();
        expect(fixture.nativeElement.querySelector('mat-card.studio-search nav[aria-label="Definition type"]')).not.toBeNull();
        expect(fixture.componentInstance.kinds).toHaveLength(13);
        expect(fixture.componentInstance.rows()).toEqual([{ id: 0, name: "Initial sword" }]);
    });
    it("pages ID ranges and links rows to their dedicated detail route", async () => {
        const fixture = await createPage(),
            page = fixture.componentInstance;
        page.browse();
        TestBed.tick();
        http.expectOne((r) => r.url === `${baseUrl}items/range`).flush([{ id: 0, name: "Sword" }]);
        await stable();
        const paginator = await TestbedHarnessEnvironment.loader(fixture).getHarness(MatPaginatorHarness);
        expect(await paginator.getRangeLabel()).toBe("1 – 10 of 23");
        const changing = paginator.goToLastPage();
        let last!: TestRequest;
        await vi.waitFor(() => {
            last = http.expectOne((r) => r.url === `${baseUrl}items/range`);
        });
        expect(last.request.params.get("startId")).toBe("20");
        expect(last.request.params.get("endIdExclusive")).toBe("23");
        last.flush([{ id: 20, name: "Last sword" }]);
        await changing;
        await stable();
        const link = fixture.nativeElement.querySelector('a[aria-label="Inspect definition 20"]') as HTMLAnchorElement;
        expect(link.getAttribute("href")).toBe("/cache/types/items/20");
        link.click();
        await stable();
        expect(TestBed.inject(Router).url).toBe("/cache/types/items/20");
    });
    it("paginates the submitted query, changes page size and resets category state", async () => {
        const fixture = await createPage(),
            page = fixture.componentInstance;
        page.searchForm.query().value.set(" coin ");
        page.browse();
        TestBed.tick();
        http.expectOne((r) => r.url === `${baseUrl}items/search`).flush({
            query: "coin",
            totalMatches: 40,
            offset: 0,
            limit: 10,
            items: [{ id: 995, name: "Coins" }],
        });
        await stable();
        page.searchForm.query().value.set("sword");
        const paginator = await TestbedHarnessEnvironment.loader(fixture).getHarness(MatPaginatorHarness);
        const changing = paginator.goToNextPage();
        let next!: TestRequest;
        await vi.waitFor(() => {
            next = http.expectOne((r) => r.url === `${baseUrl}items/search`);
        });
        expect(next.request.params.get("query")).toBe("coin");
        expect(next.request.params.get("offset")).toBe("10");
        page.changePage({ pageIndex: 2, pageSize: 10, length: 40 });
        TestBed.tick();
        http.expectNone(() => true);
        next.flush({ query: "coin", totalMatches: 40, offset: 10, limit: 10, items: [] });
        await changing;
        await stable();
        const resizing = paginator.setPageSize(25);
        let resized!: TestRequest;
        await vi.waitFor(() => {
            resized = http.expectOne((r) => r.url === `${baseUrl}items/search`);
        });
        expect(resized.request.params.get("limit")).toBe("25");
        resized.flush({ query: "coin", totalMatches: 40, offset: 0, limit: 25, items: [] });
        await resizing;
        await stable();
        page.browse();
        TestBed.tick();
        const first = http.expectOne((r) => r.url === `${baseUrl}items/search`);
        expect(first.request.params.get("query")).toBe("sword");
        expect(first.request.params.get("offset")).toBe("0");
        first.flush({ query: "sword", totalMatches: 0, offset: 0, limit: 25, items: [] });
        await stable();
        page.selectKind(page.kinds.indexOf("npcs"));
        expect(page.rows()).toEqual([]);
        expect(page.pageIndex()).toBe(0);
        expect(page.paginationLength()).toBe(40);
        expect(page.searchForm.query().value()).toBe("");
        TestBed.tick();
        const category = http.expectOne((r) => r.url === `${baseUrl}npcs/range`);
        expect(category.request.params.get("startId")).toBe("0");
        expect(category.request.params.get("endIdExclusive")).toBe("25");
        category.flush([{ id: 1, name: "Goblin" }]);
        await stable();
        expect(page.rows()).toEqual([{ id: 1, name: "Goblin" }]);
    });
    it("opens numeric lookups as dedicated pages and rejects invalid IDs", async () => {
        const fixture = await createPage(),
            page = fixture.componentInstance;
        page.selectKind(page.kinds.indexOf("npcs"));
        TestBed.tick();
        http.expectOne((r) => r.url === `${baseUrl}npcs/range`).flush([]);
        await stable();
        page.searchForm.query().value.set("0");
        page.browse();
        await stable();
        expect(TestBed.inject(Router).url).toBe("/cache/types/npcs/0");
        http.expectNone(() => true);
        page.searchForm.query().value.set("-1");
        page.browse();
        expect(page.browseError()).toContain("zero or greater");
        page.searchForm.limit().value.set(201);
        page.browse();
        expect(page.searchForm().invalid()).toBe(true);
        http.expectNone(() => true);
    });
    it.each(["maps", "cs2-int"] as const)("keeps %s as ID-only lookup", async (kind) => {
        const fixture = await createPage(),
            page = fixture.componentInstance;
        page.selectKind(page.kinds.indexOf(kind));
        page.browse();
        expect(page.browseError()).toContain("lookup by ID");
        expect(page.paginated()).toBe(false);
        page.searchForm.query().value.set("name");
        page.browse();
        expect(page.browseError()).toContain("Name search is available");
        http.expectNone(() => true);
    });
    it("shows request failures without stale rows", async () => {
        const fixture = await createPage(),
            page = fixture.componentInstance;
        page.browse();
        TestBed.tick();
        http.expectOne((r) => r.url === `${baseUrl}items/range`).flush({ detail: "Unavailable" }, { status: 503, statusText: "Unavailable" });
        await stable();
        expect(page.error()).toBe("Unavailable");
        expect(page.rows()).toEqual([]);
    });
    it("reports archive size failures and permits retry", async () => {
        const fixture = TestBed.createComponent(TypesManagerPageComponent);
        fixture.detectChanges();
        http.expectOne(`${baseUrl}archive-sizes`).flush({ detail: "Unavailable" }, { status: 503, statusText: "Unavailable" });
        await stable();
        expect(fixture.componentInstance.error()).toBe("Unavailable");
        fixture.componentInstance.archives.loadArchiveSizes();
        TestBed.tick();
        http.expectOne(`${baseUrl}archive-sizes`).flush({ items: 0 });
        await stable();
        fixture.componentInstance.browse();
        expect(fixture.componentInstance.browseError()).toContain("no definitions");
    });
});
