import { provideHttpClient, withInterceptors } from "@angular/common/http";
import { provideHttpClientTesting, HttpTestingController } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AuthService } from "./auth.service";
import { AuthStorageService } from "./auth.storage.service";
import { authInterceptor } from "./auth.interceptor";
import { AuthStore } from "./auth.store";

describe("Admin AuthStore", () => {
    let http: HttpTestingController;
    let store: AuthStore;
    let authStorage: {
        getRefreshToken: ReturnType<typeof vi.fn>;
        setRefreshToken: ReturnType<typeof vi.fn>;
        removeRefreshToken: ReturnType<typeof vi.fn>;
    };

    beforeEach(() => {
        authStorage = {
            getRefreshToken: vi.fn(() => null),
            setRefreshToken: vi.fn(),
            removeRefreshToken: vi.fn(),
        };

        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(withInterceptors([authInterceptor])),
                provideHttpClientTesting(),
                AuthService,
                AuthStore,
                { provide: AuthStorageService, useValue: authStorage },
            ],
        });

        http = TestBed.inject(HttpTestingController);
        store = TestBed.inject(AuthStore);
    });

    afterEach(() => http.verify());

    it("completes admin sign-in when the token response omits the requested scope", async () => {
        const result = await signIn({
            access_token: "access-token",
            refresh_token: "refresh-token",
            id_token: "id-token",
            expires_in: 3600,
            token_type: "Bearer",
        });

        expect(store.error()).not.toBe("Authentication failed.");
        expect(result).toBe(true);
        expect(store.error()).toBeNull();
        expect(store.isAuthorizedAdmin()).toBe(true);
        expect(authStorage.setRefreshToken).toHaveBeenCalledWith("refresh-token");
    });

    it("does not infer cache access when the token response explicitly omits cache_api", async () => {
        const result = await signIn({
            access_token: "access-token",
            refresh_token: "refresh-token",
            id_token: "id-token",
            scope: "openid offline_access email profile roles characters_api",
            expires_in: 3600,
            token_type: "Bearer",
        });

        expect(result).toBe(false);
        expect(store.error()).toBe("User is authenticated but lacks required admin permissions.");
        expect(store.isAuthorizedAdmin()).toBe(false);
    });

    async function signIn(tokenResponse: Record<string, unknown>): Promise<boolean> {
        const result = store.login("admin@example.test", "password", "captcha-token");

        http.expectOne((request) => request.url.endsWith("captcha/verify")).flush({ succeeded: true });
        await Promise.resolve();

        const tokenRequest = http.expectOne((request) => request.url.endsWith("connect/token"));
        expect(tokenRequest.request.method).toBe("POST");
        expect(tokenRequest.request.body.get("client_id")).toBe("storm-admin");
        expect(tokenRequest.request.body.get("scope")).toBe("openid offline_access email profile roles characters_api cache_api");
        tokenRequest.flush(tokenResponse);
        await Promise.resolve();

        const userInfoRequest = http.expectOne((request) => request.url.endsWith("connect/userinfo"));
        expect(userInfoRequest.request.headers.get("Authorization")).toBe("Bearer access-token");
        userInfoRequest.flush({
            sub: "admin-user",
            username: "admin",
            preferred_username: "Admin",
            email: "admin@example.test",
            email_verified: true,
            role: ["SystemAdministrator"],
        });

        return result;
    }
});
