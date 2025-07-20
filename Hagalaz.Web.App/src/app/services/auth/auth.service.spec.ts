import { TestBed } from "@angular/core/testing";
import { it, describe, expect } from "vitest";

import { AuthService } from "./auth.service";

describe("AuthService", () => {
    it("should be created", () => {
        const service: AuthService = TestBed.inject(AuthService);
        expect(service).toBeTruthy();
    });
});
