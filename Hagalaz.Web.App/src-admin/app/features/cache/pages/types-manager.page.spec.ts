import { TestbedHarnessEnvironment } from "@angular/cdk/testing/testbed";
import { Location } from "@angular/common";
import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from "@angular/common/http/testing";
import { provideLocationMocks } from "@angular/common/testing";
import { ApplicationRef, Component } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { MatPaginatorHarness } from "@angular/material/paginator/testing";
import { provideRouter, Router } from "@angular/router";
import { EMPTY } from "rxjs";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../environments/environment";
import { CacheItemsService } from "../services/cache-items.service";
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
                provideRouter([
                    { path: "", component: DetailBoundary },
                    { path: "cache/types/:kind/:id", component: DetailBoundary },
                ]),
                provideLocationMocks(),
                provideHttpClient(withXhr()),
                provideHttpClientTesting(),
            ],
        });
        vi.spyOn(TestBed.inject(CacheItemsService), "getItemSpriteImage").mockReturnValue(EMPTY);
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => {
        http.verify();
        vi.restoreAllMocks();
        vi.unstubAllGlobals();
    });
    async function createPage(sprites = 20) {
        const fixture = TestBed.createComponent(TypesManagerPageComponent);
        fixture.detectChanges();
        http.expectOne(`${baseUrl}archive-sizes`).flush({
            items: 23,
            objects: 30,
            npcs: 40,
            sprites,
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
    it("restores deep-linked search parameters and browser history without duplicate requests", async () => {
        const router = TestBed.inject(Router);
        router.setUpLocationChangeListener();
        await router.navigateByUrl("/?kind=npcs&query=goblin&offset=25&limit=25");
        const fixture = TestBed.createComponent(TypesManagerPageComponent);
        fixture.detectChanges();
        http.expectOne(`${baseUrl}archive-sizes`).flush({ npcs: 80, items: 23 });
        TestBed.tick();
        await vi.waitFor(() => {
            const request = http.expectOne((r) => r.url === `${baseUrl}npcs/search`);
            expect(request.request.params.get("query")).toBe("goblin");
            expect(request.request.params.get("offset")).toBe("25");
            expect(request.request.params.get("limit")).toBe("25");
            request.flush({ query: "goblin", totalMatches: 80, items: [] });
        });
        await stable();
        const page = fixture.componentInstance;
        expect(page.activeKind()).toBe("npcs");
        expect(page.pageIndex()).toBe(1);
        page.searchForm.query().value.set("draft");
        await page.changePage({ pageIndex: 2, pageSize: 25, length: 80 });
        TestBed.tick();
        const next = http.expectOne((r) => r.url === `${baseUrl}npcs/search`);
        expect(next.request.params.get("query")).toBe("goblin");
        expect(next.request.params.get("offset")).toBe("50");
        next.flush({ query: "goblin", totalMatches: 80, items: [] });
        await stable();
        expect(page.searchForm.query().value()).toBe("draft");
        expect(router.url).toContain("query=goblin");
        TestBed.inject(Location).back();
        await vi.waitFor(() => {
            const back = http.expectOne((r) => r.url === `${baseUrl}npcs/search`);
            expect(back.request.params.get("offset")).toBe("25");
            back.flush({ query: "goblin", totalMatches: 80, items: [] });
        });
        await stable();
        expect(page.pageIndex()).toBe(1);
        TestBed.inject(Location).forward();
        await vi.waitFor(() => {
            const forward = http.expectOne((r) => r.url === `${baseUrl}npcs/search`);
            expect(forward.request.params.get("offset")).toBe("50");
            forward.flush({ query: "goblin", totalMatches: 80, items: [] });
        });
        await stable();
        expect(page.pageIndex()).toBe(2);
        http.expectNone(() => true);
    });
    it("uses safe defaults for invalid URL parameters", async () => {
        await TestBed.inject(Router).navigateByUrl("/?kind=unknown&offset=-1&limit=201");
        const fixture = await createPage();
        expect(fixture.componentInstance.activeKind()).toBe("items");
        expect(fixture.componentInstance.pageSize()).toBe(10);
        expect(fixture.componentInstance.pageIndex()).toBe(0);
        http.expectNone(() => true);
    });
    it("normalizes an out-of-range URL to the last page without sending an invalid range", async () => {
        const fixture = await createPage();
        await TestBed.inject(Router).navigateByUrl("/?kind=items&offset=999&limit=10");
        TestBed.tick();
        await vi.waitFor(() => {
            const request = http.expectOne((r) => r.url === `${baseUrl}items/range`);
            expect(request.request.params.get("startId")).toBe("20");
            expect(request.request.params.get("endIdExclusive")).toBe("23");
            request.flush([{ id: 20 }]);
        });
        await stable();
        expect(fixture.componentInstance.pageIndex()).toBe(2);
        expect(TestBed.inject(Router).url).toContain("offset=20");
        http.expectNone(() => true);
    });
    it("shows exclusive request feedback and retries the applied page without using draft text", async () => {
        const fixture = await createPage();
        const page = fixture.componentInstance;
        await page.changePage({ pageIndex: 1, pageSize: 10, length: 23 });
        TestBed.tick();
        const delayed = http.expectOne((r) => r.url === `${baseUrl}items/range`);
        expect(fixture.nativeElement.textContent).toContain("Loading definitions");
        expect(fixture.nativeElement.textContent).not.toContain("Initial sword");
        expect(fixture.nativeElement.textContent).not.toContain("No matching definitions");
        delayed.flush({ detail: "Unavailable" }, { status: 503, statusText: "Unavailable" });
        await stable();
        expect(fixture.nativeElement.querySelector("mat-card.definition-list [role=alert]")?.textContent).toContain("Unavailable");
        expect(fixture.nativeElement.textContent).not.toContain("No matching definitions");
        page.searchForm.query().value.set("unsubmitted");
        (fixture.nativeElement.querySelector("mat-card.definition-list button") as HTMLButtonElement).click();
        TestBed.tick();
        const retry = http.expectOne((r) => r.url === `${baseUrl}items/range`);
        expect(retry.request.params.get("startId")).toBe("10");
        expect(retry.request.params.get("endIdExclusive")).toBe("20");
        retry.flush([]);
        await stable();
        expect(page.searchForm.query().value()).toBe("unsubmitted");
        expect(page.pageIndex()).toBe(1);
        expect(fixture.nativeElement.textContent).toContain("No matching definitions");
        expect(fixture.nativeElement.querySelector("[role=alert]")).toBeNull();
        expect(fixture.nativeElement.querySelector("mat-progress-bar")).toBeNull();
    });
    it("hides previous search results while a different query is delayed and retries that query", async () => {
        const fixture = await createPage();
        const page = fixture.componentInstance;
        page.searchForm.query().value.set("sword");
        await page.browse();
        TestBed.tick();
        http.expectOne((r) => r.url === `${baseUrl}items/search`).flush({ query: "sword", totalMatches: 1, items: [{ id: 1, name: "Old search sword" }] });
        await stable();
        expect(fixture.nativeElement.textContent).toContain("Old search sword");
        page.searchForm.query().value.set("coin");
        await TestBed.inject(Router).navigateByUrl("/?kind=items&query=coin&offset=25&limit=25");
        TestBed.tick();
        const delayed = http.expectOne((r) => r.url === `${baseUrl}items/search`);
        expect(delayed.request.params.get("query")).toBe("coin");
        expect(fixture.nativeElement.textContent).not.toContain("Old search sword");
        expect(fixture.nativeElement.textContent).toContain("Loading definitions");
        delayed.flush({ detail: "Search offline" }, { status: 503, statusText: "Unavailable" });
        await stable();
        page.searchForm.query().value.set("draft");
        page.retry();
        TestBed.tick();
        const retry = http.expectOne((r) => r.url === `${baseUrl}items/search`);
        expect(retry.request.params.get("query")).toBe("coin");
        expect(retry.request.params.get("offset")).toBe("25");
        expect(retry.request.params.get("limit")).toBe("25");
        retry.flush({ query: "coin", totalMatches: 1, items: [{ id: 995, name: "Coins" }] });
        await stable();
        expect(fixture.nativeElement.textContent).toContain("Coins");
        expect(page.searchForm.query().value()).toBe("draft");
        expect(TestBed.inject(Router).url).toContain("query=coin");
        http.expectNone(() => true);
    });
    it("describes exact-ID lookup for sprites and rejects unsupported name searches", async () => {
        const fixture = await createPage();
        const page = fixture.componentInstance;
        await page.selectKind(page.kinds.indexOf("sprites"));
        TestBed.tick();
        http.expectOne((r) => r.url === `${baseUrl}sprites/range`).flush([]);
        await stable();
        expect(page.searchLabel()).toBe("Definition ID");
        expect(fixture.nativeElement.querySelector('input[placeholder="Enter an exact ID…"]')).not.toBeNull();
        expect(page.searchHelp()).toContain("ID ranges");
        page.searchForm.query().value.set("name");
        await page.browse();
        TestBed.tick();
        expect(page.browseError()).toContain("Name search is available for items and NPCs");
        http.expectNone((r) => r.url === `${baseUrl}sprites/search`);
    });
    it("automatically loads the first ten definitions and keeps tabs inside the search card", async () => {
        const fixture = await createPage();
        expect(fixture.nativeElement.querySelector('mat-card.studio-search nav[aria-label="Definition type"]')).not.toBeNull();
        expect(fixture.componentInstance.kinds).toHaveLength(13);
        expect(fixture.componentInstance.rows()).toEqual([{ id: 0, name: "Initial sword" }]);
    });
    it("pages ID ranges and links rows to their dedicated detail route", async () => {
        const fixture = await createPage(),
            page = fixture.componentInstance;
        await page.browse();
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
        expect(link.getAttribute("href")).toContain("/cache/types/items/20?");
        link.click();
        await stable();
        expect(TestBed.inject(Router).url).toContain("/cache/types/items/20?");
    });
    it("paginates the submitted query, changes page size and resets category state", async () => {
        const fixture = await createPage(),
            page = fixture.componentInstance;
        page.searchForm.query().value.set(" coin ");
        await page.browse();
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
        await page.changePage({ pageIndex: 2, pageSize: 10, length: 40 });
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
        await page.browse();
        TestBed.tick();
        const first = http.expectOne((r) => r.url === `${baseUrl}items/search`);
        expect(first.request.params.get("query")).toBe("sword");
        expect(first.request.params.get("offset")).toBe("0");
        first.flush({ query: "sword", totalMatches: 0, offset: 0, limit: 25, items: [] });
        await stable();
        await page.selectKind(page.kinds.indexOf("npcs"));
        expect(page.rows()).toEqual([]);
        expect(page.pageIndex()).toBe(0);
        TestBed.tick();
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
        await page.selectKind(page.kinds.indexOf("npcs"));
        TestBed.tick();
        http.expectOne((r) => r.url === `${baseUrl}npcs/range`).flush([]);
        await stable();
        page.searchForm.query().value.set("0");
        await page.browse();
        await stable();
        expect(TestBed.inject(Router).url).toContain("/cache/types/npcs/0?");
        http.expectNone(() => true);
        page.searchForm.query().value.set("-1");
        await page.browse();
        expect(page.browseError()).toContain("zero or greater");
        http.expectNone(() => true);
    });
    it.each(["maps", "cs2-int"] as const)("keeps %s as ID-only lookup", async (kind) => {
        const fixture = await createPage(),
            page = fixture.componentInstance;
        await page.selectKind(page.kinds.indexOf(kind));
        await page.browse();
        expect(page.browseError()).toContain("lookup by ID");
        expect(page.paginated()).toBe(false);
        page.searchForm.query().value.set("name");
        await page.browse();
        TestBed.tick();
        expect(page.browseError()).toContain("Name search is available");
        http.expectNone(() => true);
    });
    it("shows request failures without stale rows", async () => {
        const fixture = await createPage(),
            page = fixture.componentInstance;
        await page.browse();
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
        expect(fixture.nativeElement.textContent).not.toContain("No matching definitions");
        fixture.componentInstance.retry();
        TestBed.tick();
        http.expectOne(`${baseUrl}archive-sizes`).flush({ items: 0 });
        await stable();
        fixture.componentInstance.browse();
        expect(fixture.componentInstance.browseError()).toContain("no definitions");
    });
    it("previews frame one in sprite rows and handles single-frame and empty sprites", async () => {
        vi.stubGlobal(
            "URL",
            class extends URL {
                static override createObjectURL = vi.fn(() => "blob:table-sprite");
                static override revokeObjectURL = vi.fn();
            }
        );
        const fixture = await createPage();
        const page = fixture.componentInstance;
        await page.selectKind(page.kinds.indexOf("sprites"));
        TestBed.tick();
        await vi.waitFor(() =>
            http
                .expectOne((r) => r.url === `${baseUrl}sprites/range`)
                .flush([
                    { id: 10, width: 32, height: 32, frameCount: 4 },
                    { id: 11, width: 32, height: 32, frameCount: 1 },
                    { id: 12, width: 0, height: 0, frameCount: 0 },
                ])
        );
        await vi.waitFor(() => {
            const requests = http.match((r) => r.url.endsWith("/image"));
            expect(requests).toHaveLength(2);
            expect(requests.find((r) => r.request.url.endsWith("/10/image"))?.request.params.get("frame")).toBe("1");
            expect(requests.find((r) => r.request.url.endsWith("/11/image"))?.request.params.get("frame")).toBe("0");
            for (const request of requests) request.flush(new Blob(["png"], { type: "image/png" }));
        });
        await stable();
        expect(page.resultColumns()).toEqual(["id", "preview", "name"]);
        expect(fixture.nativeElement.querySelectorAll("td cache-sprite-frame-thumbnail img")).toHaveLength(2);
        expect(fixture.nativeElement.textContent).toContain("No frames");
        expect(fixture.nativeElement.querySelector('a[href^="/cache/types/sprites/10?"]')).not.toBeNull();
        fixture.destroy();
        expect(URL.revokeObjectURL).toHaveBeenCalledTimes(2);
    });
    it("renders an item sprite thumbnail in item rows", async () => {
        const fixture = await createPage();
        fixture.detectChanges();

        expect(fixture.componentInstance.resultColumns()).toEqual(["id", "preview", "name"]);
        expect(fixture.nativeElement.querySelector("th.mat-column-preview")?.textContent).toContain("Item sprite");
        expect(fixture.nativeElement.querySelector("td cache-item-sprite-thumbnail")).not.toBeNull();
        expect(fixture.nativeElement.querySelector("td img")).toBeNull();
    });
    it("pages sprites without gaps, caps the last range, and cancels previews from the previous page", async () => {
        let urlIndex = 0;
        vi.stubGlobal(
            "URL",
            class extends URL {
                static override createObjectURL = vi.fn(() => `blob:sprite-${urlIndex++}`);
                static override revokeObjectURL = vi.fn();
            }
        );
        const fixture = await createPage(23);
        const page = fixture.componentInstance;
        const spriteRows = (start: number, count: number) => Array.from({ length: count }, (_, index) => ({ id: start + index, frameCount: 2 }));
        const ids: number[] = [];
        await page.selectKind(page.kinds.indexOf("sprites"));
        TestBed.tick();
        await vi.waitFor(() => {
            const request = http.expectOne((r) => r.url === `${baseUrl}sprites/range`);
            expect(request.request.params.get("startId")).toBe("0");
            expect(request.request.params.get("endIdExclusive")).toBe("10");
            request.flush(spriteRows(0, 10));
        });
        let oldPreview!: TestRequest;
        await vi.waitFor(() => {
            const previews = http.match((r) => r.url.endsWith("/image"));
            expect(previews).toHaveLength(10);
            oldPreview = previews[0];
            for (const preview of previews.slice(1)) preview.flush(new Blob(["png"], { type: "image/png" }));
        });
        ids.push(...page.rows().map((row) => row.id));
        (fixture.nativeElement.querySelector('button[aria-label="Next page"]') as HTMLButtonElement).click();
        TestBed.tick();
        await vi.waitFor(() => {
            const request = http.expectOne((r) => r.url === `${baseUrl}sprites/range`);
            expect(request.request.params.get("startId")).toBe("10");
            expect(request.request.params.get("endIdExclusive")).toBe("20");
            request.flush(spriteRows(10, 10));
        });
        expect(oldPreview.cancelled).toBe(true);
        await vi.waitFor(() => {
            const previews = http.match((r) => r.url.endsWith("/image"));
            expect(previews).toHaveLength(10);
            expect(previews.map((r) => Number(r.request.url.split("/").at(-2)))).toEqual(Array.from({ length: 10 }, (_, index) => index + 10));
            for (const preview of previews) {
                expect(preview.request.params.get("frame")).toBe("1");
                preview.flush(new Blob(["png"], { type: "image/png" }));
            }
        });
        await stable();
        ids.push(...page.rows().map((row) => row.id));
        const paginator = await TestbedHarnessEnvironment.loader(fixture).getHarness(MatPaginatorHarness);
        expect(await paginator.getRangeLabel()).toBe("11 – 20 of 23");
        const changing = paginator.goToLastPage();
        await vi.waitFor(() => {
            const request = http.expectOne((r) => r.url === `${baseUrl}sprites/range`);
            expect(request.request.params.get("startId")).toBe("20");
            expect(request.request.params.get("endIdExclusive")).toBe("23");
            request.flush(spriteRows(20, 3));
        });
        await vi.waitFor(() => {
            const previews = http.match((r) => r.url.endsWith("/image"));
            expect(previews).toHaveLength(3);
            expect(previews.map((r) => Number(r.request.url.split("/").at(-2)))).toEqual([20, 21, 22]);
            for (const preview of previews) preview.flush(new Blob(["png"], { type: "image/png" }));
        });
        await changing;
        ids.push(...page.rows().map((row) => row.id));
        expect(ids).toEqual(Array.from({ length: 23 }, (_, index) => index));
        expect(await paginator.getRangeLabel()).toBe("21 – 23 of 23");
        expect(await paginator.isNextPageDisabled()).toBe(true);
        (fixture.nativeElement.querySelector('button[aria-label="Next page"]') as HTMLButtonElement).click();
        http.expectNone(() => true);
        fixture.destroy();
        expect(URL.revokeObjectURL).toHaveBeenCalledTimes(urlIndex);
    });

    it("keeps range pagination aligned when resizing the last page and navigating back", async () => {
        const fixture = await createPage(23);
        const page = fixture.componentInstance;
        await page.selectKind(page.kinds.indexOf("sprites"));
        TestBed.tick();
        await vi.waitFor(() => http.expectOne((r) => r.url === `${baseUrl}sprites/range`).flush([{ id: 0, frameCount: 0 }]));
        await stable();
        const paginator = await TestbedHarnessEnvironment.loader(fixture).getHarness(MatPaginatorHarness);
        const last = paginator.goToLastPage();
        await vi.waitFor(() => {
            const request = http.expectOne((r) => r.url === `${baseUrl}sprites/range`);
            expect(request.request.params.get("startId")).toBe("20");
            request.flush([
                { id: 20, frameCount: 0 },
                { id: 21, frameCount: 0 },
                { id: 22, frameCount: 0 },
            ]);
        });
        await last;
        const resizing = paginator.setPageSize(25);
        await vi.waitFor(() => {
            const request = http.expectOne((r) => r.url === `${baseUrl}sprites/range`);
            expect(request.request.params.get("startId")).toBe("0");
            expect(request.request.params.get("endIdExclusive")).toBe("23");
            request.flush(Array.from({ length: 23 }, (_, id) => ({ id, frameCount: 0 })));
        });
        await resizing;
        expect(page.pageIndex()).toBe(0);
        expect(page.rows()).toHaveLength(23);
        expect(await paginator.getRangeLabel()).toBe("1 – 23 of 23");
        const shrinking = paginator.setPageSize(10);
        await vi.waitFor(() => {
            const request = http.expectOne((r) => r.url === `${baseUrl}sprites/range`);
            expect(request.request.params.get("startId")).toBe("0");
            expect(request.request.params.get("endIdExclusive")).toBe("10");
            request.flush([{ id: 0, frameCount: 0 }]);
        });
        await shrinking;
        expect(await paginator.getRangeLabel()).toBe("1 – 10 of 23");
        expect(await paginator.isPreviousPageDisabled()).toBe(true);
    });
    it("disables next and last page when the sprite archive contains one result", async () => {
        const fixture = await createPage(1);
        const page = fixture.componentInstance;
        await page.selectKind(page.kinds.indexOf("sprites"));
        TestBed.tick();
        await vi.waitFor(() => {
            const request = http.expectOne((r) => r.url === `${baseUrl}sprites/range`);
            expect(request.request.params.get("startId")).toBe("0");
            expect(request.request.params.get("endIdExclusive")).toBe("1");
            request.flush([{ id: 0, frameCount: 0 }]);
        });
        await stable();
        const paginator = await TestbedHarnessEnvironment.loader(fixture).getHarness(MatPaginatorHarness);
        expect(page.rows()).toHaveLength(1);
        expect(page.paginationLength()).toBe(1);
        expect(await paginator.getRangeLabel()).toBe("1 – 1 of 1");
        expect(await paginator.isNextPageDisabled()).toBe(true);
        expect(fixture.nativeElement.querySelector('button[aria-label="Last page"]')?.getAttribute("aria-disabled")).toBe("true");
        (fixture.nativeElement.querySelector('button[aria-label="Next page"]') as HTMLButtonElement).click();
        (fixture.nativeElement.querySelector('button[aria-label="Last page"]') as HTMLButtonElement).click();
        expect(page.pageIndex()).toBe(0);
        http.expectNone(() => true);
    });
});
