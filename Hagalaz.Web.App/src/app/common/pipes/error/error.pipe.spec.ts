import { ErrorPipe } from "./error.pipe";
import { it, describe, expect } from "vitest";

describe("ErrorPipe", () => {
    it("create an instance", () => {
        const pipe = new ErrorPipe();
        expect(pipe).toBeTruthy();
    });
});
