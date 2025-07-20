import { TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { AuthStorageService } from "@app/services/auth/auth.storage.service";

describe("AuthStorageService", () => {
    let service: AuthStorageService;

    beforeEach(() => {
        service = TestBed.inject(AuthStorageService);
    });

    it("should be created", () => {
        expect(service).toBeTruthy();
    });
});
