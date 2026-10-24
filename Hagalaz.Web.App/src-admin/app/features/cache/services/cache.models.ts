export type TypeKind =
    | "items"
    | "npcs"
    | "objects"
    | "sprites"
    | "quests"
    | "animations"
    | "graphics"
    | "maps"
    | "varp-bits"
    | "client-map-definitions"
    | "config-definitions"
    | "cs2"
    | "cs2-int";

export interface ArchiveSizesDto {
    items: number;
    npcs: number;
    objects: number;
    sprites: number;
    quests: number;
    animations: number;
    graphics: number;
    cs2: number;
    clientMapDefinitions: number;
    configDefinitions: number;
    varpBits: number;
    maps: number;
}

export interface SearchResultDto<T> {
    query: string;
    totalMatches: number;
    offset: number;
    limit: number;
    items: T[];
}

export interface DefinitionSummaryDto {
    id: number;
    name?: string;
}

/** Read-only cache definition. Server prices, bonuses and instance state are separate. */
export interface ItemDefinitionDto {
    id: number;
    name?: string;
    value?: number | null;
    stackable?: boolean | null;
    noted?: boolean | null;
    noteId?: number | null;
    membersOnly?: boolean | null;
    equipSlot?: number | null;
    equipType?: number | null;
    renderAnimationId?: number | null;
    inventoryOptions?: (string | null)[] | null;
    groundOptions?: (string | null)[] | null;
    noteTemplateId?: number | null;
    lendId?: number | null;
    lendTemplateId?: number | null;
    boughtItemId?: number | null;
    boughtTemplateId?: number | null;
    interfaceModelId?: number | null;
}

export interface TypeMutationResultDto {
    operation: string;
    type: string;
    id: number;
}

export interface SpriteMutationResultDto extends TypeMutationResultDto {
    width: number;
    height: number;
    frameCount: number;
}
export const cacheTypeLabels: Record<TypeKind, string> = {
    items: "Items",
    npcs: "NPCs",
    objects: "Objects",
    sprites: "Sprites",
    quests: "Quests",
    animations: "Animations",
    graphics: "Graphics",
    maps: "Maps",
    "varp-bits": "Varp bits",
    "client-map-definitions": "Client map definitions",
    "config-definitions": "Config definitions",
    cs2: "CS2 scripts",
    "cs2-int": "CS2 integers",
};

export const cacheTypeNames: Record<TypeKind, string> = {
    items: "Item",
    npcs: "NPC",
    objects: "Object",
    sprites: "Sprite",
    quests: "Quest",
    animations: "Animation",
    graphics: "Graphic",
    maps: "Map",
    "varp-bits": "Varp bit",
    "client-map-definitions": "Client map definition",
    "config-definitions": "Config definition",
    cs2: "CS2 script",
    "cs2-int": "CS2 integer",
};

export interface SpriteMetadataDto {
    id: number;
    width: number;
    height: number;
    frameCount: number;
}
