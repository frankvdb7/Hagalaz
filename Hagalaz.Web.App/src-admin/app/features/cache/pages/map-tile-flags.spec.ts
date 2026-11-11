import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import { blockedPlane, collisionPlane, isBridgeColumn, isStorableFlags, tileFlag, tileFlagFacts, tileFlagOptions } from "./map-tile-flags";

const none = { blocked: false, bridge: false, roof: false, drawOnGroundPlane: false, hiddenOnMinimap: false };
const planes = (...columns: number[]) => columns.map(value => ({ flags: [value] }));
// Tests run from the web app folder; the fixtures are shared with the cache service's tests.
const fixture = <T>(name: string): T => JSON.parse(readFileSync(resolve(process.cwd(), "../TestSupport/Fixtures/maps", name), "utf8")) as T;

describe("tileFlagFacts", () => {
    it("reports nothing for a tile without flags", () => {
        expect(tileFlagFacts(0)).toEqual(none);
    });

    it.each([
        [1, { blocked: true }],
        [2, { bridge: true }],
        [4, { roof: true }],
        [8, { drawOnGroundPlane: true }],
        [16, { hiddenOnMinimap: true }],
        [5, { blocked: true, roof: true }],
        [29, { blocked: true, roof: true, drawOnGroundPlane: true, hiddenOnMinimap: true }],
        [32, {}],
    ])("names the bits of value %i", (value, facts) => {
        expect(tileFlagFacts(value)).toEqual({ ...none, ...facts });
    });

    it("offers each named bit once, blocked first", () => {
        expect(tileFlagOptions.map(option => option.bit)).toEqual([1, 2, 4, 8, 16]);
        expect(new Set(tileFlagOptions.map(option => option.rgb)).size).toBe(5);
        expect(tileFlagOptions.map(option => tileFlag[option.name])).toEqual([1, 2, 4, 8, 16]);
    });
});

describe("isStorableFlags", () => {
    it("accepts the values one opcode can carry", () => {
        expect([0, 1, 31, 32].every(isStorableFlags)).toBe(true);
    });

    it("refuses values with no stored form", () => {
        expect([-1, 33, 64, 255, 1.5, Number.NaN].some(isStorableFlags)).toBe(false);
    });
});

describe("collision planes", () => {
    it("blocks the tile's own plane away from a bridge", () => {
        expect(blockedPlane(planes(1, 0, 1, 1), 0, 0)).toBe(0);
        expect(blockedPlane(planes(1, 0, 1, 1), 2, 0)).toBe(2);
        expect(blockedPlane(planes(0, 4, 0, 0), 1, 0)).toBeNull();
    });

    it("moves blocking one plane down on a plane 1 bridge and drops it below the ground", () => {
        const bridge = planes(1, 3, 1, 1);
        expect(isBridgeColumn(bridge, 0)).toBe(true);
        expect(blockedPlane(bridge, 1, 0)).toBe(0);
        expect(blockedPlane(bridge, 3, 0)).toBe(2);
        expect(blockedPlane(bridge, 0, 0)).toBeNull();
        expect(collisionPlane(bridge, 0, 0)).toBeNull();
    });

    it("ignores the bridge bit on other planes", () => {
        const elsewhere = planes(3, 1, 2, 2);
        expect(isBridgeColumn(elsewhere, 0)).toBe(false);
        expect(blockedPlane(elsewhere, 0, 0)).toBe(0);
        expect(collisionPlane(elsewhere, 3, 0)).toBe(3);
    });

    it("treats a missing plane as unflagged", () => {
        expect(isBridgeColumn([{ flags: [3] }], 0)).toBe(false);
        expect(blockedPlane([{ flags: [1] }], 2, 0)).toBeNull();
    });
});

describe("agreement with the game server on region 12850", () => {
    const flags = fixture<number[][]>("flags-12850.json");
    const serverBlocked = fixture<number[]>("blocked-12850.json");

    it("blocks exactly the tiles the server blocks on each collision plane", () => {
        const region = flags.map(plane => ({ flags: plane }));
        const blocked: number[] = [];
        region.forEach((_, plane) => {
            for (let tile = 0; tile < 4096; tile++) {
                const target = blockedPlane(region, plane, tile);
                if (target !== null) blocked.push((target << 12) | tile);
            }
        });
        expect(blocked.sort((left, right) => left - right)).toEqual(serverBlocked);
    });

    it("covers the bridge rule and every named flag", () => {
        const region = flags.map(plane => ({ flags: plane }));
        const dropped = flags[0].filter((value, tile) => (value & tileFlag.blocked) !== 0 && isBridgeColumn(region, tile)).length;
        expect(dropped).toBe(32);
        expect(flags[1].filter((_, tile) => isBridgeColumn(region, tile))).toHaveLength(40);
        const used = new Set(flags.flat());
        for (const { bit } of tileFlagOptions) expect([...used].some(value => (value & bit) !== 0)).toBe(true);
    });
});
