import { TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { LauncherService } from "./launcher.service";

describe("LauncherService", () => {
    let service: LauncherService;

    beforeEach(() => {
        service = TestBed.inject(LauncherService);
    });

    it("should be created", () => {
        expect(service).toBeTruthy();
    });
});
