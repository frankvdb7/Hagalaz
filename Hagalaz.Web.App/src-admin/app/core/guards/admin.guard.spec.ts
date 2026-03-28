import { TestBed } from "@angular/core/testing";
import { provideRouter, Router, UrlTree } from "@angular/router";
import { describe, expect, it } from "vitest";
import { adminCanActivate } from "./admin.guard";
import { AuthStore } from "../auth/auth.store";

describe("adminCanActivate", () => {
    function configure(authenticated: boolean, authorized: boolean): Router {
        TestBed.configureTestingModule({
            providers: [
                provideRouter([]),
                {
                    provide: AuthStore,
                    useValue: {
                        authenticated: () => authenticated,
                        isAuthorizedAdmin: () => authorized,
                    },
                },
            ],
        });

        return TestBed.inject(Router);
    }

    it("redirects to login when unauthenticated", () => {
        const router = configure(false, false);
        const result = TestBed.runInInjectionContext(() => adminCanActivate({} as never, {} as never));

        expect(result instanceof UrlTree).toBe(true);
        expect(router.serializeUrl(result as UrlTree)).toBe("/login");
    });

    it("redirects to denied when authenticated without admin access", () => {
        const router = configure(true, false);
        const result = TestBed.runInInjectionContext(() => adminCanActivate({} as never, {} as never));

        expect(result instanceof UrlTree).toBe(true);
        expect(router.serializeUrl(result as UrlTree)).toBe("/denied");
    });

    it("allows navigation for authorized system admin", () => {
        configure(true, true);
        const result = TestBed.runInInjectionContext(() => adminCanActivate({} as never, {} as never));

        expect(result).toBe(true);
    });
});
