import { describe, expect, it } from "vitest";
import { getCacheErrorMessage } from "./cache-error";

describe("getCacheErrorMessage", () => {
    it.each([
        {
            name: "problem detail",
            error: { error: { detail: "Definition not found.", title: "Not found" }, message: "HTTP failure" },
            expected: "Definition not found.",
        },
        { name: "problem title", error: { error: { title: "Cache is read-only." }, message: "HTTP failure" }, expected: "Cache is read-only." },
        { name: "transport message", error: new Error("Connection interrupted."), expected: "Connection interrupted." },
        { name: "null response body", error: { error: null, message: "HTTP failure" }, expected: "HTTP failure" },
        { name: "generic fallback", error: {}, expected: "Request failed." },
    ])("selects the $name", ({ error, expected }) => {
        expect(getCacheErrorMessage(error)).toBe(expected);
    });
});
