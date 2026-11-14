/**
 * Terrain tile flags as the revision-742 client reads them. One stored opcode carries the whole value, so a
 * tile holds 0 (no token) to 32; bit 32 has no reader and cannot be combined with the others.
 */
export const tileFlag = {
    /** Unwalkable on the tile's collision plane. */
    blocked: 1,
    /** On plane 1, moves the column's collision one plane down; the minimap and camera read it on planes 0 and 3. */
    bridge: 2,
    /** Standing here hides the planes above connected roof tiles. */
    roof: 4,
    /** The minimap draws the tile with plane 0, and it stays visible when roofs are hidden. */
    drawOnGroundPlane: 8,
    /** The minimap skips the tile. */
    hiddenOnMinimap: 16,
} as const;

export type TileFlagName = keyof typeof tileFlag;
export type TileFlagFacts = Record<TileFlagName, boolean>;

/** In overlay priority order: where a tile has several flags, the first listed colours it. */
export const tileFlagOptions: readonly { name: TileFlagName; bit: number; label: string; rgb: number }[] = [
    { name: "blocked", bit: tileFlag.blocked, label: "Blocked", rgb: 0xff2d2d },
    { name: "bridge", bit: tileFlag.bridge, label: "Bridge", rgb: 0x2d7dff },
    { name: "roof", bit: tileFlag.roof, label: "Roof", rgb: 0xc040ff },
    { name: "drawOnGroundPlane", bit: tileFlag.drawOnGroundPlane, label: "Draw on plane 0", rgb: 0x00d0c0 },
    { name: "hiddenOnMinimap", bit: tileFlag.hiddenOnMinimap, label: "Hidden on minimap", rgb: 0xb0b0b0 },
];

const largestStoredFlags = 32;

export function tileFlagFacts(value: number): TileFlagFacts {
    return {
        blocked: (value & tileFlag.blocked) !== 0,
        bridge: (value & tileFlag.bridge) !== 0,
        roof: (value & tileFlag.roof) !== 0,
        drawOnGroundPlane: (value & tileFlag.drawOnGroundPlane) !== 0,
        hiddenOnMinimap: (value & tileFlag.hiddenOnMinimap) !== 0,
    };
}

export function isStorableFlags(value: number): boolean {
    return Number.isInteger(value) && value >= 0 && value <= largestStoredFlags;
}

type FlagPlanes = readonly ({ readonly flags: readonly number[] } | undefined)[];

/** Whether collision in this column lands one plane lower, which only plane 1 decides. */
export function isBridgeColumn(planes: FlagPlanes, tile: number): boolean {
    return ((planes[1]?.flags[tile] ?? 0) & tileFlag.bridge) !== 0;
}

/**
 * The plane that anything on this tile collides on, as the client and the game server place it: one lower
 * on a bridge column, and nowhere (null) when that falls below the ground plane.
 */
export function collisionPlane(planes: FlagPlanes, plane: number, tile: number): number | null {
    const target = isBridgeColumn(planes, tile) ? plane - 1 : plane;
    return target >= 0 ? target : null;
}

/** The plane a tile's blocked flag makes unwalkable, or null when the tile blocks nothing. */
export function blockedPlane(planes: FlagPlanes, plane: number, tile: number): number | null {
    if (((planes[plane]?.flags[tile] ?? 0) & tileFlag.blocked) === 0) return null;
    return collisionPlane(planes, plane, tile);
}
