import { describe, expect, it } from "vitest";
import { mapFogRange, selectedMapIds } from "./map-render.models";

describe("map selection", () => {
    it("packs adjacent coordinates without treating archive IDs as regions", () => {
        expect(selectedMapIds({ id: 258, width: 2, height: 2 })).toEqual([258, 259, 514, 515]);
        expect(selectedMapIds({ id: 0, width: 3, height: 3 })).toEqual([0, 1, 2, 256, 257, 258, 512, 513, 514]);
        expect(selectedMapIds({ id: 65535, width: 1, height: 1 })).toEqual([65535]);
    });
    it.each([{ id: -1, width: 1, height: 1 }, { id: 65536, width: 1, height: 1 }, { id: 255, width: 1, height: 2 }, { id: 65280, width: 2, height: 1 }, { id: 0, width: 4, height: 1 }, { id: 0, width: 1, height: 0 }, { id: 0, width: 1.5, height: 1 }])("rejects invalid or wrapping selection %j", selection => {
        expect(selectedMapIds(selection)).toEqual([]);
    });
});

describe("map fog range", () => {
    it("ends at the client far plane and widens with the raw fog value", () => {
        expect(mapFogRange(0)).toEqual({ start: 13308, end: 14332 });
        expect(mapFogRange(600)).toEqual({ start: 10908, end: 14332 });
    });
    it("clamps the start to the client near plane", () => {
        expect(mapFogRange(65535)).toEqual({ start: 200, end: 14332 });
    });
});
