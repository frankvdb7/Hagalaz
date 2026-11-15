import { describe, expect, it } from "vitest";
import { waterTurbulence, waterVolume } from "./water-noise";

describe("client water noise", () => {
    it.each([
        ["ridged", 524288, [23, 23, 21, 21, 15, 15, 10, 10], 12],
        ["lake", 524288, [127, 127, 162, 162, 170, 170, 166, 166], 169],
        ["normal", 1048576, [121, 124, 255, 124, 122, 125, 255, 132], 116],
    ] as const)("reproduces the %s volume and shares its data", async (kind, length, first, last) => {
        const pending = waterVolume(kind);
        expect(waterVolume(kind)).toBe(pending);
        const data = await pending;
        expect(data).toHaveLength(length);
        expect(Array.from(data.subarray(0, first.length))).toEqual(first);
        expect(data.at(-1)).toBe(last);
    });

    it("packs the two identical tables as pairs for the rows the client window reads", () => {
        const data = waterTurbulence();
        expect(data).toHaveLength(256);
        expect(Array.from(data.slice(0, 4), value => value * 4096)).toEqual([0, 0, -687, -687]);
        expect(Array.from(data.slice(126, 132), value => value * 4096)).toEqual([565, 565, -159, -159, -834, -834]);
        expect(waterTurbulence()).toBe(data);
    });
});
