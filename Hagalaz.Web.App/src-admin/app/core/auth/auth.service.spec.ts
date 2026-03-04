import { TestBed } from "@angular/core/testing";
import { provideHttpClient } from "@angular/common/http";
import { provideHttpClientTesting, HttpTestingController } from "@angular/common/http/testing";
import { describe, expect, it } from "vitest";
import { AuthService } from "./auth.service";

describe("Admin AuthService", () => {
    it("requests token with cache_api scope", () => {
        TestBed.configureTestingModule({
            providers: [provideHttpClient(), provideHttpClientTesting(), AuthService],
        });

        const service = TestBed.inject(AuthService);
        const http = TestBed.inject(HttpTestingController);

        service.login("admin@hagalaz.dev", "secret").subscribe();

        const request = http.expectOne((req) => req.url.endsWith("connect/token"));
        expect(request.request.method).toBe("POST");

        const scope = request.request.body.get("scope");
        expect(scope).toContain("cache_api");

        request.flush({
            access_token: "token",
            refresh_token: "refresh",
            id_token: "id",
            scope: "openid cache_api",
            expires_in: 3600,
            token_type: "Bearer",
        });

        http.verify();
    });
});
