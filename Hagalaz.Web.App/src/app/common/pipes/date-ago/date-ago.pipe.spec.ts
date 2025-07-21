import { DateAgoPipe } from "./date-ago.pipe";
import { it, describe, expect } from "vitest";

describe("DateAgoPipe", () => {
    it("create an instance", () => {
        const pipe = new DateAgoPipe();
        expect(pipe).toBeTruthy();
    });
});
