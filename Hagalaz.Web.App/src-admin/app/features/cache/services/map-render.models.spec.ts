import { describe, expect, it } from "vitest";
import { parseMapKeys, selectedMapIds } from "./map-render.models";

describe("map selection and keys", () => {
    it("packs adjacent coordinates without treating archive IDs as regions", () => {
        expect(selectedMapIds({ id: 258, width: 2, height: 2 })).toEqual([258, 259, 514, 515]);
        expect(selectedMapIds({ id: 0, width: 3, height: 3 })).toEqual([0, 1, 2, 256, 257, 258, 512, 513, 514]);
        expect(selectedMapIds({ id: 65535, width: 1, height: 1 })).toEqual([65535]);
    });
    it.each([{ id: -1, width: 1, height: 1 }, { id: 65536, width: 1, height: 1 }, { id: 255, width: 1, height: 2 }, { id: 65280, width: 2, height: 1 }, { id: 0, width: 4, height: 1 }, { id: 0, width: 1, height: 0 }, { id: 0, width: 1.5, height: 1 }])("rejects invalid or wrapping selection %j", selection => {
        expect(selectedMapIds(selection)).toEqual([]);
    });
    it("accepts optional signed region keys including zero keys", () => {
        expect(parseMapKeys("", [258])).toEqual({});
        expect(parseMapKeys('{"258":[-2147483648,2147483647,0,-1]}', [258])).toEqual({ 258: [-2147483648, 2147483647, 0, -1] });
    });
    it.each(['null', '[]', '{"258":[1,2,3]}', '{"258":[1,2,3,0.5]}', '{"258":[1,2,3,2147483648]}', '{"259":[0,0,0,0]}', '{"0258":[0,0,0,0]}', '{'])("rejects invalid key input %s", text => {
        expect(() => parseMapKeys(text, [258])).toThrow();
    });
});
