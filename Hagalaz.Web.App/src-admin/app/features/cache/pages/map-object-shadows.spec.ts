import { describe, expect, it } from "vitest";
import type { MapPlacement, MapRenderRegion, ObjectRenderAppearance } from "../services/map-render.models";
import { mapAppearanceKey } from "../services/map-render.models";
import { noObjectShadows, objectShadowField, shadowedFloorColor } from "./map-object-shadows";

const region = { id: 258, regionX: 1, regionY: 2, objects: [] } as unknown as MapRenderRegion;
const originX = 64, originY = 128;
const appearance = (placement: MapPlacement, shadowStrength: number, sizeX = 1, sizeY = 1) => ({ id: placement.id, shape: placement.shape, rotation: placement.rotation, sizeX, sizeY, decorDisplacement: 64, contourMode: 0, contourParameter: -1, parts: [], warnings: [], shadowStrength }) as ObjectRenderAppearance;
const placed = (shape: number, rotation: number, x = 10, y = 20, plane = 0, id = 7): MapPlacement => ({ id, shape, rotation, x, y, plane });
const field = (entries: readonly [MapPlacement, ObjectRenderAppearance][], moved?: Pick<MapPlacement, "x" | "y" | "plane" | "rotation">) =>
    objectShadowField([region], new Map(entries.map(([placement, value]) => [mapAppearanceKey(placement), value])), entries.map(([placement]) => ({ placement, regionId: region.id, moved })));

// A lone source of 50 filters to 25 on itself, 12 to the east and north, 6 to the west and south.
const expectedAround = (read: (x: number, y: number) => number, x: number, y: number) => {
    expect(read(x, y)).toBeGreaterThanOrEqual(25);
};

describe("object shadow field", () => {
    const read = (shadows: ReturnType<typeof field>) => (x: number, y: number) => shadows.value(0, originX + x, originY + y);

    it.each([
        [0, 0, [[10, 20], [10, 21]]],
        [0, 1, [[10, 21], [11, 21]]],
        [0, 2, [[11, 20], [11, 21]]],
        [0, 3, [[10, 20], [11, 20]]],
        [1, 0, [[10, 21]]],
        [1, 1, [[11, 21]]],
        [1, 2, [[11, 20]]],
        [1, 3, [[10, 20]]],
        [3, 0, [[10, 21]]],
        [3, 3, [[10, 20]]],
    ])("writes wall shape %i rotation %i to its client corners", (shape, rotation, corners) => {
        const placement = placed(shape, rotation);
        const at = read(field([[placement, appearance(placement, 15)]]));
        for (const [x, y] of corners) expectedAround(at, x, y);
        expect(at(13, 23)).toBe(0);
    });

    it("applies the client's asymmetric integer filter", () => {
        const source = placed(10, 0, 10, 20);
        const at = read(field([[source, appearance(source, 30, 0, 0)]]));
        expect(at(10, 20)).toBe(15);
        expect(at(11, 20)).toBe(7); // Reads its western neighbour at >>2.
        expect(at(9, 20)).toBe(3); // Reads its eastern neighbour at >>3.
        expect(at(10, 21)).toBe(7);
        expect(at(10, 19)).toBe(3);
    });

    it("covers a ground object's whole footprint inclusive of its far edges, swapped by rotation", () => {
        const straight = placed(10, 0);
        const wide = read(field([[straight, appearance(straight, 20, 3, 1)]]));
        expect(wide(13, 21)).toBeGreaterThan(0);
        expect(wide(10, 23)).toBe(0);
        const turned = placed(10, 1);
        const tall = read(field([[turned, appearance(turned, 20, 3, 1)]]));
        expect(tall(11, 23)).toBeGreaterThan(0);
        expect(tall(13, 20)).toBe(0);
    });

    it("keeps the strongest write instead of adding", () => {
        const weak = placed(10, 0, 10, 20, 0, 1), strong = placed(10, 0, 10, 20, 0, 2);
        const both = field([[weak, appearance(weak, 10, 0, 0)], [strong, appearance(strong, 30, 0, 0)]]);
        expect(both.value(0, originX + 10, originY + 20)).toBe(15);
    });

    it("interpolates between corners", () => {
        const source = placed(10, 0);
        const shadows = field([[source, appearance(source, 30, 0, 0)]]);
        expect(shadows.value(0, originX + 10.5, originY + 20)).toBeCloseTo((15 + 7) / 2);
        expect(shadows.value(0, originX + 10.5, originY + 20.5)).toBeCloseTo(((15 + 7) / 2 + (7 + 0) / 2) / 2);
    });

    it("casts nothing for disabled, undecorated or underwater objects, and keeps planes apart", () => {
        const disabled = placed(10, 0);
        expect(field([[disabled, appearance(disabled, 0)]])).toBe(noObjectShadows);
        const decoration = placed(22, 0);
        expect(field([[decoration, appearance(decoration, 30)]]).value(0, originX + 10, originY + 20)).toBe(0);
        const underwater = placed(10, 0, 10, 20, 4);
        expect(field([[underwater, appearance(underwater, 30)]])).toBe(noObjectShadows);
        const upper = placed(10, 0, 10, 20, 1);
        const shadows = field([[upper, appearance(upper, 30, 0, 0)]]);
        expect(shadows.value(1, originX + 10, originY + 20)).toBe(15);
        expect(shadows.value(0, originX + 10, originY + 20)).toBe(0);
    });

    it("follows a draft move and gives equal fields equal signatures", () => {
        const source = placed(10, 0);
        const value = appearance(source, 30, 0, 0);
        const moved = field([[source, value]], { x: 30, y: 40, plane: 0, rotation: 0 });
        expect(moved.value(0, originX + 30, originY + 40)).toBe(15);
        expect(moved.value(0, originX + 10, originY + 20)).toBe(0);
        expect(field([[source, value]]).signature).toBe(field([[source, value]]).signature);
        expect(moved.signature).not.toBe(field([[source, value]]).signature);
    });

    it("darkens relative to unshadowed ground", () => {
        expect(shadowedFloorColor([0.5, 0.25, 1], 0)).toEqual([0.5, 0.25, 1]);
        const shaded = shadowedFloorColor([0.5, 0.25, 1], 37);
        expect(shaded[0]).toBeCloseTo(0.5 * 0.5 ** 1.54);
        expect(shadowedFloorColor([1, 1, 1], 200)).toEqual([0, 0, 0]);
    });
});
