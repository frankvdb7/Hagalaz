import { TestBed } from "@angular/core/testing";
import { provideHttpClient, withXhr } from "@angular/common/http";
import { provideHttpClientTesting, HttpTestingController } from "@angular/common/http/testing";
import { it, describe, expect } from "vitest";

import { AuthService } from "./auth.service";

describe("AuthService", () => {
    it("should be created", () => {
        const service: AuthService = TestBed.inject(AuthService);
        expect(service).toBeTruthy();
    });

    it("requests tokens with the normal app client and without the admin scope", () => {
        TestBed.configureTestingModule({
            providers: [provideHttpClient(withXhr()), provideHttpClientTesting(), AuthService],
        });

        const service = TestBed.inject(AuthService);
        const http = TestBed.inject(HttpTestingController);

        service.login({ email: "player@hagalaz.dev", password: "secret" }).subscribe();

        const request = http.expectOne((req) => req.url.endsWith("connect/token"));
        expect(request.request.method).toBe("POST");
        expect(request.request.body.get("client_id")).toBe("storm-app");
        expect(request.request.body.get("scope")).toBe("openid offline_access email profile roles characters_api");

        request.flush({
            access_token: "token",
            refresh_token: "refresh",
            id_token: "id",
            scope: "openid characters_api",
            expires_in: 3600,
            token_type: "Bearer",
        });

        http.verify();
    });
});
