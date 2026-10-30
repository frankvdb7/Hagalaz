import { Location } from "@angular/common";
import { provideLocationMocks } from "@angular/common/testing";
import { BreakpointObserver } from "@angular/cdk/layout";
import { TestbedHarnessEnvironment } from "@angular/cdk/testing/testbed";
import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { ApplicationRef, Component, input, signal } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { MatAutocompleteHarness } from "@angular/material/autocomplete/testing";
import { MatDialog } from "@angular/material/dialog";
import { MatMenuHarness } from "@angular/material/menu/testing";
import { MatSidenav } from "@angular/material/sidenav";
import { By } from "@angular/platform-browser";
import { provideRouter, Router } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { of } from "rxjs";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { PortalShellPageComponent } from "./layout/portal-shell.page";
import { routes } from "./app.routes";
import { AuthStore } from "./core/auth/auth.store";
import { CacheModelPreviewComponent } from "./features/cache/pages/cache-model-preview.component";
import { NpcsDetailPageComponent } from "./features/cache/pages/details/npcs-detail.page";
import { ItemsDetailPageComponent } from "./features/cache/pages/details/items-detail.page";

@Component({ template: "<h1>Login boundary</h1>" })
class LoginBoundary {}

@Component({ selector: "cache-model-preview", template: "" })
class ModelPreviewBoundary {
    readonly definition = input<unknown>();
    readonly modelId = input<number | null>(null);
    readonly itemId = input<number | null>(null);
}

async function openRoute(path: string): Promise<RouterTestingHarness> {
    const harness = await RouterTestingHarness.create(path);
    TestBed.tick();
    TestBed.inject(HttpTestingController)
        .match((req) => req.url.endsWith("/archive-sizes"))
        .forEach((req) => {
            req.flush({ items: 23, npcs: 20 });
        });
    if (path === "/cache/types") {
        await vi.waitFor(() => {
            TestBed.inject(HttpTestingController)
                .expectOne((req) => req.url.endsWith("/items/range"))
                .flush([]);
        });
    }
    await TestBed.inject(ApplicationRef).whenStable();
    return harness;
}

function routeElement(harness: RouterTestingHarness): HTMLElement {
    const element = harness.routeNativeElement;
    if (!element) throw new Error("Expected an activated portal route");
    return element;
}

describe("admin portal routing", () => {
    const authenticated = signal(true);
    const authorized = signal(true);
    const logout = vi.fn(async () => {
        expect(TestBed.inject(Router).url).toBe("/login");
        authenticated.set(false);
        authorized.set(false);
    });

    beforeEach(() => {
        authenticated.set(true);
        authorized.set(true);
        logout.mockClear();
        TestBed.configureTestingModule({
            providers: [
                provideRouter(routes.map((route) => (route.path === "login" ? { path: "login", component: LoginBoundary } : route))),
                provideLocationMocks(),
                provideHttpClient(withXhr()),
                provideHttpClientTesting(),
                {
                    provide: AuthStore,
                    useValue: { authenticated, isAuthorizedAdmin: authorized, user: signal({ preferred_username: "Test administrator" }), logout },
                },
            ],
        });
        TestBed.overrideComponent(NpcsDetailPageComponent, { remove: { imports: [CacheModelPreviewComponent] }, add: { imports: [ModelPreviewBoundary] } });
        TestBed.overrideComponent(ItemsDetailPageComponent, { remove: { imports: [CacheModelPreviewComponent] }, add: { imports: [ModelPreviewBoundary] } });
    });

    it("opens the overview with real branding and honest telemetry states", async () => {
        const harness = await openRoute("/");
        expect(TestBed.inject(Router).url).toBe("/overview");
        expect(routeElement(harness).querySelector("h1")?.textContent).toContain("The realm answers to you");
        expect(harness.routeNativeElement?.textContent).toContain("Metrics not connected");
        expect(routeElement(harness).querySelector('img[src="/assets/icons/logo.svg"]')).not.toBeNull();
        expect(routeElement(harness).querySelectorAll('nav[aria-label="Administration"] a')).toHaveLength(8);
    });

    it("uses only the main toolbar breadcrumb for Sprite Studio", async () => {
        const opening = RouterTestingHarness.create("/cache/types/sprites/0");
        await vi.waitFor(() =>
            TestBed.inject(HttpTestingController)
                .expectOne((req) => req.url.endsWith("/sprites/0"))
                .flush({ id: 0, width: 0, height: 0, frameCount: 0 })
        );
        const harness = await opening;
        const element = routeElement(harness);
        expect(element.querySelectorAll('nav[aria-label="Breadcrumb"]')).toHaveLength(1);
        const breadcrumb = element.querySelector('mat-toolbar nav[aria-label="Breadcrumb"]');
        expect(breadcrumb?.textContent).toContain("Sprite #0");
        expect(breadcrumb?.querySelector('a[href="/cache/types?kind=sprites"]')).not.toBeNull();
        expect(element.querySelector("cache-sprites-detail-page nav")).toBeNull();
    });

    it("uses the main toolbar breadcrumb for Item Forge", async () => {
        const opening = RouterTestingHarness.create("/cache/types/items/0");
        const http = TestBed.inject(HttpTestingController);
        await vi.waitFor(() => {
            const requests = http.match((req) => req.url.endsWith("/items/0"));
            expect(requests.length).toBe(1);
            for (const request of requests) request.flush({ id: 0, name: "First item" });
        });
        const harness = await opening;
        const element = routeElement(harness);
        expect(element.querySelectorAll('nav[aria-label="Breadcrumb"]')).toHaveLength(1);
        const breadcrumb = element.querySelector('mat-toolbar nav[aria-label="Breadcrumb"]');
        await vi.waitFor(() => expect(breadcrumb?.textContent).toContain("First item #0"));
        expect(breadcrumb?.querySelector('a[href="/cache/types?kind=items"]')).not.toBeNull();
        expect(element.querySelector("cache-items-detail-page nav[aria-label=Breadcrumb]")).toBeNull();
    });

    it.each(["/cache/types", "/cache/sprites", "/characters", "/worlds", "/economy", "/infrastructure", "/audit", "/settings"])(
        "preserves the guarded destination %s",
        async (path) => {
            const harness = await openRoute(path);
            expect(TestBed.inject(Router).url).toBe(path);
            expect(routeElement(harness).querySelector('[aria-current="page"]')?.textContent).toContain(
                path.startsWith("/cache")
                    ? "Cache Studio"
                    : path === "/audit"
                      ? "Audit Logs"
                      : path === "/worlds"
                        ? "Worlds"
                        : path.slice(1, 2).toUpperCase() + path.slice(2)
            );
            expect(routeElement(harness).querySelector("main")?.textContent).toContain(path.startsWith("/cache") ? "Browse game definitions" : "not connected");
        }
    );

    it("redirects an unauthenticated deep link to login without a portal", async () => {
        authenticated.set(false);
        const harness = await openRoute("/cache/sprites");
        expect(TestBed.inject(Router).url).toBe("/login");
        expect(harness.routeNativeElement?.textContent).toContain("Login boundary");
        expect(routeElement(harness).querySelector("mat-sidenav-container")).toBeNull();
    });

    it("rechecks authorization between portal children", async () => {
        const harness = await openRoute("/overview");
        authorized.set(false);
        await harness.navigateByUrl("/cache/types");
        expect(TestBed.inject(Router).url).toBe("/denied");
        expect(routeElement(harness).querySelector("mat-sidenav-container")).toBeNull();
    });

    it("resets the shared content scroll position for a newly activated page", async () => {
        const harness = await openRoute("/cache/types");
        const content = routeElement(harness).querySelector("mat-sidenav-content");
        if (!content) throw new Error("Expected the shared content scroller");
        content.scrollTop = 350;
        await harness.navigateByUrl("/overview");
        expect(content.scrollTop).toBe(0);
        expect(routeElement(harness).querySelector("a.skip-link")?.getAttribute("href")).toBe("/overview#portal-content");
    });

    it("closes the mobile drawer after selecting a destination", async () => {
        TestBed.overrideProvider(BreakpointObserver, { useValue: { observe: () => of({ matches: true, breakpoints: {} }) } });
        const harness = await openRoute("/overview");
        const drawer = harness.fixture.debugElement.query(By.directive(MatSidenav)).componentInstance as MatSidenav;
        expect(drawer.mode).toBe("over");
        expect(drawer.opened).toBe(false);
        (routeElement(harness).querySelector('button[aria-label="Open navigation"]') as HTMLButtonElement).click();
        await harness.fixture.whenStable();
        expect(drawer.opened).toBe(true);
        (routeElement(harness).querySelector('a[href="/worlds"]') as HTMLAnchorElement).click();
        await harness.fixture.whenStable();
        expect(TestBed.inject(Router).url).toBe("/worlds");
        expect(drawer.opened).toBe(false);
    });

    it("replaces numeric deep-link history so Back does not reopen the definition", async () => {
        await openRoute("/cache/types");
        const router = TestBed.inject(Router);
        router.setUpLocationChangeListener();
        await router.navigateByUrl("/cache/types?kind=items&query=7");
        const http = TestBed.inject(HttpTestingController);
        await vi.waitFor(() =>
            http.expectOne((r) => r.url.endsWith("/items/7")).flush({ id: 7, name: "Deep-linked item", inventoryOptions: [], groundOptions: [] })
        );
        await TestBed.inject(ApplicationRef).whenStable();
        expect(router.url).toContain("/cache/types/items/7?");
        expect(router.url).not.toContain("query=7");
        TestBed.inject(Location).back();
        await vi.waitFor(() => http.expectOne((r) => r.url.endsWith("/archive-sizes")).flush({ items: 23 }));
        await vi.waitFor(() => http.expectOne((r) => r.url.endsWith("/items/range")).flush([]));
        await TestBed.inject(ApplicationRef).whenStable();
        expect(router.url).toBe("/cache/types");
        http.expectNone((r) => r.url.endsWith("/items/7"));
    });
    it("opens the modern sprite browser from Overview and global page search", async () => {
        const harness = await openRoute("/overview");
        const link = routeElement(harness).querySelector<HTMLAnchorElement>('a[href="/cache/types?kind=sprites"]');
        expect(link?.textContent).toContain("Edit sprites");
        const http = TestBed.inject(HttpTestingController);
        async function flushSprites() {
            await vi.waitFor(() => http.expectOne((r) => r.url.endsWith("/archive-sizes")).flush({ sprites: 1 }));
            await vi.waitFor(() => http.expectOne((r) => r.url.endsWith("/sprites/range")).flush([{ id: 0, frameCount: 0 }]));
            await TestBed.inject(ApplicationRef).whenStable();
            expect(TestBed.inject(Router).url).toBe("/cache/types?kind=sprites");
            expect(routeElement(harness).querySelector('button[aria-selected="true"]')?.textContent).toContain("Sprites");
        }
        link?.click();
        await flushSprites();
        await harness.navigateByUrl("/overview");
        const search = await TestbedHarnessEnvironment.loader(harness.fixture).getHarness(MatAutocompleteHarness);
        await search.enterText("sprites");
        expect(await Promise.all((await search.getOptions()).map((option) => option.getText()))).toEqual(["Sprites"]);
        const selecting = search.selectOption({ text: /Sprites/ });
        await flushSprites();
        await selecting;
        await harness.navigateByUrl("/cache/sprites");
        expect(routeElement(harness).querySelector("sprites-manager-page")).not.toBeNull();
        expect(routeElement(harness).textContent).not.toContain("Find sprites by name");
        http.verify();
    });
    it("searches existing workspace destinations and navigates to the selected page", async () => {
        const harness = await openRoute("/cache/types");
        const loader = TestbedHarnessEnvironment.loader(harness.fixture);
        const search = await loader.getHarness(MatAutocompleteHarness);
        await search.enterText("world");
        expect(await Promise.all((await search.getOptions()).map((option) => option.getText()))).toEqual(["Worlds"]);
        await search.selectOption({ text: /Worlds/ });
        expect(TestBed.inject(Router).url).toBe("/worlds");
        expect(await search.getValue()).toBe("");
        expect(routeElement(harness).querySelector(".route-title")?.textContent).toBe("Worlds");
    });

    it("shows an honest notification state and keeps settings reachable", async () => {
        const harness = await openRoute("/cache/types");
        const loader = TestbedHarnessEnvironment.loader(harness.fixture);
        const menu = await loader.getHarness(MatMenuHarness.with({ triggerText: "notifications_none" }));
        await menu.open();
        expect(document.body.textContent).toContain("Notifications are not connected.");
        await menu.close();
        (routeElement(harness).querySelector('a[aria-label="Settings"]') as HTMLAnchorElement).click();
        await harness.fixture.whenStable();
        expect(TestBed.inject(Router).url).toBe("/settings");
    });

    it("shows search feedback when no workspace destination matches", async () => {
        const harness = await openRoute("/cache/types");
        const search = await TestbedHarnessEnvironment.loader(harness.fixture).getHarness(MatAutocompleteHarness);
        await search.enterText("unknown realm page");
        const options = await search.getOptions();
        expect(await options[0].getText()).toBe("No matching pages");
        expect(await options[0].isDisabled()).toBe(true);
        expect(TestBed.inject(Router).url).toBe("/cache/types");
    });

    it.each([false, true])("resolves dirty navigation before logout (discard=%s)", async (discard) => {
        const opening = RouterTestingHarness.create("/cache/types/npcs/7");
        await vi.waitFor(() =>
            TestBed.inject(HttpTestingController)
                .expectOne((r) => r.url.endsWith("/npcs/7"))
                .flush({ id: 7, combatLevel: 3, name: "NPC" })
        );
        const harness = await opening;
        await TestBed.inject(ApplicationRef).whenStable();
        const input = routeElement(harness).querySelector<HTMLInputElement>("cache-npcs-editor input");
        if (!input) throw new Error("Expected NPC combat editor");
        input.value = "99";
        input.dispatchEvent(new Event("input", { bubbles: true }));
        await harness.fixture.whenStable();
        const shell = harness.fixture.debugElement.query(By.directive(PortalShellPageComponent)).componentInstance as PortalShellPageComponent;
        const exiting = shell.logout();
        await vi.waitFor(() => expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(1));
        expect(logout).not.toHaveBeenCalled();
        TestBed.inject(MatDialog).openDialogs[0].close(discard);
        await exiting;
        expect(logout).toHaveBeenCalledTimes(discard ? 1 : 0);
        expect(authenticated()).toBe(!discard);
        expect(TestBed.inject(Router).url).toBe(discard ? "/login" : "/cache/types/npcs/7");
        if (!discard) expect(input.value).toBe("99");
    });
    it("does not log out when login navigation is rejected or redirected", async () => {
        const harness = await openRoute("/overview");
        const shell = harness.fixture.debugElement.query(By.directive(PortalShellPageComponent)).componentInstance as PortalShellPageComponent;
        const router = TestBed.inject(Router);
        const base = router.config.filter((route) => route.path !== "login");
        router.resetConfig([{ path: "login", component: LoginBoundary, canActivate: [() => false] }, ...base]);
        await shell.logout();
        expect(logout).not.toHaveBeenCalled();
        expect(authenticated()).toBe(true);
        router.resetConfig([{ path: "login", redirectTo: "overview" }, ...base]);
        await shell.logout();
        expect(logout).not.toHaveBeenCalled();
        expect(authenticated()).toBe(true);
    });
    it("signs out through the account menu and existing auth owner and returns to login", async () => {
        const harness = await openRoute("/overview");
        expect(routeElement(harness).querySelector(".account-avatar")?.textContent).toBe("TA");
        const menu = await TestbedHarnessEnvironment.loader(harness.fixture).getHarness(MatMenuHarness.with({ triggerText: "TA" }));
        await menu.open();
        await menu.clickItem({ text: /Sign out/ });
        await harness.fixture.whenStable();
        expect(logout).toHaveBeenCalledExactlyOnceWith();
        expect(TestBed.inject(Router).url).toBe("/login");
    });
});
