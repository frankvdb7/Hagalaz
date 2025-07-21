import { TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { UserService } from "./user.service";

describe("UserService", () => {
    let service: UserService;

    beforeEach(() => {
        TestBed.configureTestingModule({});
        service = TestBed.inject(UserService);
    });

    it("should be created", () => {
        expect(service).toBeTruthy();
    });
});
