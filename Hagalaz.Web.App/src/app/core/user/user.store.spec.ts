import { TestBed } from "@angular/core/testing";
import { UserStore } from "./user.store";
import { UserService } from "@app/services/user/user.service";
import { of, throwError, delay, timer } from "rxjs";
import { it, describe, expect, beforeEach, vi } from "vitest";
import { patchState } from "@ngrx/signals";

describe("UserStore", () => {
    let userServiceMock: any;

    beforeEach(() => {
        userServiceMock = {
            getUserInfo: vi.fn(),
        };

        TestBed.configureTestingModule({
            providers: [
                { provide: UserService, useValue: userServiceMock },
                UserStore,
            ],
        });
    });

    it("should initialize with default state", () => {
        const store = TestBed.inject(UserStore);
        expect(store.loading()).toBe(false);
        expect(store.error()).toBe(null);
        expect(store.info()).toBe(null);
        expect(store.username()).toBe("Unknown");
        expect(store.displayName()).toBe("Unknown");
    });

    it("should set loading to true when loadUser is called", () => {
        const store = TestBed.inject(UserStore);
        userServiceMock.getUserInfo.mockReturnValue(of({ username: "test" }).pipe(delay(100)));

        store.loadUser();

        expect(store.loading()).toBe(true);
        expect(store.error()).toBe(null);
    });

    it("should update info and set loading to false on success", async () => {
        const store = TestBed.inject(UserStore);
        const userInfo = { username: "testuser", preferred_username: "Test Display" };
        userServiceMock.getUserInfo.mockReturnValue(of(userInfo));

        store.loadUser();

        expect(store.info()).toEqual(userInfo);
        expect(store.username()).toBe("testuser");
        expect(store.displayName()).toBe("Test Display");
        expect(store.loading()).toBe(false);
    });

    it("should set error and set loading to false on failure", () => {
        const store = TestBed.inject(UserStore);
        const error = new Error("Failed to fetch");
        userServiceMock.getUserInfo.mockReturnValue(throwError(() => error));

        store.loadUser();

        expect(store.error()).toBe(error);
        expect(store.loading()).toBe(false);
    });

    it("should handle timeout error", async () => {
        const store = TestBed.inject(UserStore);
        // Delay response longer than the 2500ms timeout
        userServiceMock.getUserInfo.mockReturnValue(of({}).pipe(delay(3000)));

        vi.useFakeTimers();
        store.loadUser();

        // Fast forward time to trigger timeout
        vi.advanceTimersByTime(2600);

        expect(store.error()).toBeDefined();
        expect(store.loading()).toBe(false);
        vi.useRealTimers();
    });
});
