import { BreakpointObserver } from "@angular/cdk/layout";
import { TestbedHarnessEnvironment } from "@angular/cdk/testing/testbed";
import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { ApplicationRef, Component, signal } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { MatAutocompleteHarness } from "@angular/material/autocomplete/testing";
import { MatMenuHarness } from "@angular/material/menu/testing";
import { MatSidenav } from "@angular/material/sidenav";
import { By } from "@angular/platform-browser";
import { provideRouter, Router } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { of } from "rxjs";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { routes } from "./app.routes";
import { AuthStore } from "./core/auth/auth.store";

@Component({ template: "<h1>Login boundary</h1>" })
class LoginBoundary {}

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
                provideHttpClient(withXhr()),
                provideHttpClientTesting(),
                {
                    provide: AuthStore,
                    useValue: { authenticated, isAuthorizedAdmin: authorized, user: signal({ preferred_username: "Test administrator" }), logout },
                },
            ],
        });
    });

    it("opens the overview with real branding and honest telemetry states", async () => {
        const harness = await openRoute("/");
        expect(TestBed.inject(Router).url).toBe("/overview");
        expect(routeElement(harness).querySelector("h1")?.textContent).toContain("The realm answers to you");
        expect(harness.routeNativeElement?.textContent).toContain("Metrics not connected");
        expect(routeElement(harness).querySelector('img[src="/assets/icons/logo.svg"]')).not.toBeNull();
        expect(routeElement(harness).querySelectorAll('nav[aria-label="Administration"] a')).toHaveLength(8);
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
