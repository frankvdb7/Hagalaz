import { underwaterPlaneIndex, type MapPlacement, type MapRenderRegion } from "../services/map-render.models";

export interface ScenePlacement { placement: MapPlacement; regionId: number; }
export interface SceneGroup { id: string; label: string; singular: string; icon: string; shapes: readonly number[]; }
export interface SceneTreeNode {
    key: string;
    kind: "root" | "terrain" | "group" | "placement" | "more";
    label: string;
    icon: string;
    group?: SceneGroup;
    placement?: ScenePlacement;
    children?: SceneTreeNode[];
}

// The client's object shapes: 0-3 and 9 walls, 4-8 wall decoration, 10-11 ground objects, 12-21 roofs, 22 ground decoration.
export const sceneGroups: readonly SceneGroup[] = [
    { id: "walls", label: "Walls", singular: "Wall", icon: "fence", shapes: [0, 1, 2, 3, 9] },
    { id: "wall-decoration", label: "Wall decoration", singular: "Wall decoration", icon: "wallpaper", shapes: [4, 5, 6, 7, 8] },
    { id: "objects", label: "Objects", singular: "Object", icon: "chair", shapes: [10, 11] },
    { id: "roofs", label: "Roofs", singular: "Roof", icon: "roofing", shapes: [12, 13, 14, 15, 16, 17, 18, 19, 20, 21] },
    { id: "ground-decoration", label: "Ground decoration", singular: "Ground decoration", icon: "grass", shapes: [22] },
];

export function shapeLabel(shape: number): string {
    const group = sceneGroups.find(candidate => candidate.shapes.includes(shape));
    return group ? `${shape} (${group.singular})` : String(shape);
}

export interface SceneTreeSource {
    regions: readonly MapRenderRegion[];
    placements: readonly ScenePlacement[];
    query: string;
    limit: number;
    names: ReadonlyMap<number, string>;
    /** Stays listed beyond the row limit, so a selection from the map always has a row. */
    selected?: MapPlacement;
}

function placementLabel(id: number, names: ReadonlyMap<number, string>): string {
    return names.get(id) ?? `Object ${id}`;
}

function placementNode(item: ScenePlacement, names: ReadonlyMap<number, string>, group: SceneGroup): SceneTreeNode {
    const { id, x, y, plane } = item.placement;
    return { key: `placement:${item.regionId}:${id}:${x}:${y}:${plane}:${item.placement.shape}:${item.placement.rotation}`, kind: "placement", label: placementLabel(id, names), icon: "deployed_code", group, placement: item };
}

function groupNode(group: SceneGroup, items: readonly ScenePlacement[], source: SceneTreeSource): SceneTreeNode {
    const shown = items.slice(0, source.limit);
    const pinned = items.slice(source.limit).find(item => item.placement === source.selected);
    if (pinned) shown.push(pinned);
    const children = shown.map(item => placementNode(item, source.names, group));
    if (items.length > shown.length) children.push({ key: `more:${group.id}`, kind: "more", label: `Show more (${items.length - shown.length})`, icon: "expand_more", group });
    return { key: `group:${group.id}`, kind: "group", label: `${group.label} (${items.length})`, icon: group.icon, group, children };
}

/** Lists source placements, not drawn ones, so a hidden group keeps its rows and can be shown again. */
export function buildSceneTree(source: SceneTreeSource): SceneTreeNode[] {
    if (!source.regions.length) return [];
    const query = source.query.trim().toLowerCase();
    const matching = query
        ? source.placements.filter(({ placement }) => String(placement.id).includes(query) || source.names.get(placement.id)?.toLowerCase().includes(query))
        : source.placements;
    const groups = sceneGroups.flatMap(group => {
        const items = matching.filter(({ placement }) => group.shapes.includes(placement.shape));
        return items.length ? [groupNode(group, items, source)] : [];
    });
    const [first, ...others] = source.regions;
    const label = others.length ? `Regions ${first.id} +${others.length}` : `Region ${first.id}`;
    return [{ key: "root", kind: "root", label, icon: "deployed_code", children: [{ key: "terrain", kind: "terrain", label: "Terrain", icon: "landscape" }, ...groups] }];
}

/** A scene tree row that describes something other than a placement. */
export type SceneSelection = "region" | "terrain";
export interface SceneDetails { id: number; title: string; icon: string; rows: readonly { label: string; value: string }[]; warnings: readonly string[]; }

const capitalized = (value: string): string => value.charAt(0).toUpperCase() + value.slice(1);

/** What the Inspector shows for a region, as stored in the cache. */
export function regionDetails(region: MapRenderRegion): SceneDetails {
    const underwater = region.objects.filter(placement => placement.plane === underwaterPlaneIndex).length;
    return {
        id: region.id,
        title: `Region ${region.id}`,
        icon: "map",
        warnings: region.warnings,
        rows: [
            { label: "Region ID", value: String(region.id) },
            { label: "Coordinates", value: `${region.regionX}, ${region.regionY}` },
            { label: "World origin", value: `${region.regionX * 64}, ${region.regionY * 64}` },
            { label: "Terrain", value: capitalized(region.terrainStatus) },
            { label: "Landscape", value: capitalized(region.landscapeStatus) },
            { label: "Objects", value: (region.objects.length - underwater).toLocaleString("en-US") },
            ...underwater ? [{ label: "Underwater objects", value: underwater.toLocaleString("en-US") }] : [],
        ],
    };
}

/** What the Inspector shows for a region's terrain, as stored in the cache. */
export function terrainDetails(region: MapRenderRegion): SceneDetails {
    const surface = region.planes.slice(0, underwaterPlaneIndex);
    const details = { id: region.id, title: `Terrain ${region.id}`, icon: "landscape", warnings: [] };
    const status = { label: "Status", value: capitalized(region.terrainStatus) };
    if (!surface.length) return { ...details, rows: [status] };
    const count = (value: number): string => value.toLocaleString("en-US");
    const underlays = surface.flatMap(plane => plane.underlays).filter(Boolean);
    const overlays = surface.flatMap(plane => plane.overlays).filter(Boolean);
    const heights = surface[0].cornerHeights.flatMap(height => height === null ? [] : [0 - height]);
    return {
        ...details,
        rows: [
            status,
            { label: "Planes with floors", value: `${surface.filter(plane => plane.underlays.some(Boolean) || plane.overlays.some(Boolean)).length} of ${surface.length}` },
            { label: "Floor tiles", value: count(surface.reduce((total, plane) => total + plane.underlays.filter((underlay, tile) => underlay || plane.overlays[tile]).length, 0)) },
            { label: "Overlay tiles", value: count(overlays.length) },
            { label: "Underlays used", value: count(new Set(underlays).size) },
            { label: "Overlays used", value: count(new Set(overlays).size) },
            { label: "Ground height", value: heights.length ? `${count(Math.min(...heights))} to ${count(Math.max(...heights))}` : "Unknown" },
            { label: "Underwater terrain", value: region.planes[underwaterPlaneIndex] ? "Yes" : "No" },
        ],
    };
}

export function scenePlacements(regions: readonly MapRenderRegion[], visiblePlanes: readonly boolean[], additions: readonly ScenePlacement[], removed: (placement: MapPlacement) => boolean): ScenePlacement[] {
    const source = regions.flatMap(region => region.objects.map(placement => ({ placement, regionId: region.id })));
    return [...source, ...additions].filter(({ placement }) => visiblePlanes[placement.plane] && !removed(placement));
}
