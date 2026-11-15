import { describe, expect, it } from "vitest";
import type { MapPlacement, MapRenderPlane, MapRenderRegion } from "../services/map-render.models";
import { buildSceneTree, regionDetails, terrainDetails, scenePlacements, shapeLabel, type ScenePlacement } from "./map-scene-tree";

const placement = (id: number, shape: number, plane = 0): MapPlacement => ({ id, shape, rotation: 0, x: id, y: 0, plane });
const region = (id: number, objects: MapPlacement[] = []): MapRenderRegion => ({ id, regionX: 0, regionY: id, terrainStatus: "available", landscapeStatus: "available", planes: [], objects, floors: [], warnings: [] });
const items = (...values: MapPlacement[]): ScenePlacement[] => values.map(value => ({ placement: value, regionId: 1 }));
const source = (placements: ScenePlacement[], overrides = {}) => ({ regions: [region(1)], placements, query: "", limit: 50, names: new Map<number, string>(), ...overrides });

describe("map scene tree", () => {
    it("describes a region as stored, counting underwater objects apart", () => {
        const details = regionDetails({ ...region(12850, [placement(1, 10), placement(2, 10), { ...placement(3, 10), plane: 4 }]), regionX: 50, regionY: 50, landscapeStatus: "failed", warnings: ["Checked"] });
        expect(details.id).toBe(12850);
        expect(details.warnings).toEqual(["Checked"]);
        expect(details.rows).toEqual([
            { label: "Region ID", value: "12850" },
            { label: "Coordinates", value: "50, 50" },
            { label: "World origin", value: "3200, 3200" },
            { label: "Terrain", value: "Available" },
            { label: "Landscape", value: "Failed" },
            { label: "Objects", value: "2" },
            { label: "Underwater objects", value: "1" },
        ]);
    });

    it("describes terrain across the surface planes only", () => {
        const plane = (underlays: number[], overlays: number[], heights: (number | null)[]): MapRenderPlane => ({ cornerHeights: heights, flags: [], underlays, overlays, overlayShapes: [], overlayRotations: [] });
        const empty = plane([0, 0], [0, 0], []);
        const details = terrainDetails({ ...region(7), planes: [plane([3, 3], [0, 9], [0, -1280, null]), plane([0, 4], [0, 0], []), empty, empty, plane([99, 99], [99, 99], [])] });
        expect(details).toMatchObject({ id: 7, title: "Terrain 7", icon: "landscape" });
        expect(details.rows).toEqual([
            { label: "Status", value: "Available" },
            { label: "Planes with floors", value: "2 of 4" },
            { label: "Floor tiles", value: "3" },
            { label: "Overlay tiles", value: "1" },
            { label: "Underlays used", value: "2" },
            { label: "Overlays used", value: "1" },
            { label: "Ground height", value: "0 to 1,280" },
            { label: "Underwater terrain", value: "Yes" },
        ]);
    });

    it("reports only the status when a region has no terrain", () => {
        expect(terrainDetails({ ...region(7), terrainStatus: "absent" }).rows).toEqual([{ label: "Status", value: "Absent" }]);
    });

    it("omits underwater objects when a region has none", () => {
        expect(regionDetails(region(1)).rows.map(row => row.label)).not.toContain("Underwater objects");
    });

    it("is empty until a region loads", () => {
        expect(buildSceneTree(source([], { regions: [] }))).toEqual([]);
    });
    it("groups placements by client shape and always lists terrain", () => {
        const [root] = buildSceneTree(source(items(placement(1, 0), placement(2, 10), placement(3, 22), placement(4, 9))));
        expect(root.label).toBe("Region 1");
        expect(root.children?.map(node => node.label)).toEqual(["Terrain", "Walls (2)", "Objects (1)", "Ground decoration (1)"]);
        expect(root.children?.map(node => node.icon)).toEqual(["landscape", "fence", "chair", "grass"]);
    });
    it("names multi-region selections and uses object names", () => {
        const [root] = buildSceneTree(source(items(placement(7, 10)), { regions: [region(1), region(2), region(3)], names: new Map([[7, "Crate"]]) }));
        expect(root.label).toBe("Regions 1 +2");
        expect(root.children?.[1].children?.[0].label).toBe("Crate");
    });
    it("filters by ID or name", () => {
        const placements = items(placement(11, 10), placement(22, 10));
        const names = new Map([[22, "Door"]]);
        expect(buildSceneTree(source(placements, { query: "11", names }))[0].children?.[1].label).toBe("Objects (1)");
        expect(buildSceneTree(source(placements, { query: " door ", names }))[0].children?.[1].children?.[0].label).toBe("Door");
        expect(buildSceneTree(source(placements, { query: "nothing", names }))[0].children?.map(node => node.key)).toEqual(["terrain"]);
    });
    it("limits rows, offers the remainder and keeps a selection beyond the limit", () => {
        const values = [placement(1, 10), placement(2, 10), placement(3, 10)];
        const limited = buildSceneTree(source(items(...values), { limit: 1 }))[0].children?.[1].children;
        expect(limited?.map(node => node.label)).toEqual(["Object 1", "Show more (2)"]);
        const pinned = buildSceneTree(source(items(...values), { limit: 1, selected: values[2] }))[0].children?.[1].children;
        expect(pinned?.map(node => node.label)).toEqual(["Object 1", "Object 3", "Show more (1)"]);
    });
    it("gives distinct placements of one object distinct keys", () => {
        const children = buildSceneTree(source(items(placement(1, 10), { ...placement(1, 10), y: 5 })))[0].children?.[1].children;
        expect(new Set(children?.map(node => node.key)).size).toBe(2);
    });
    it("lists source placements and additions on visible planes without removed ones", () => {
        const kept = placement(1, 10), hiddenPlane = placement(2, 10, 1), removed = placement(3, 10), added = placement(4, 10);
        const result = scenePlacements([region(5, [kept, hiddenPlane, removed])], [true, false], [{ placement: added, regionId: 5 }], candidate => candidate === removed);
        expect(result).toEqual([{ placement: kept, regionId: 5 }, { placement: added, regionId: 5 }]);
    });
    it("labels shapes by their client class", () => {
        expect(shapeLabel(10)).toBe("10 (Object)");
        expect(shapeLabel(22)).toBe("22 (Ground decoration)");
        expect(shapeLabel(99)).toBe("99");
    });
});
