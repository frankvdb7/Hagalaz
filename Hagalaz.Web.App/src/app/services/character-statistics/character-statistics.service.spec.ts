import { TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { CharacterStatisticsService } from "./character-statistics.service";

describe("CharacterStatisticsService", () => {
    beforeEach(() => TestBed.configureTestingModule({}));

    it("should be created", () => {
        const service: CharacterStatisticsService = TestBed.inject(CharacterStatisticsService);
        expect(service).toBeTruthy();
    });
});
