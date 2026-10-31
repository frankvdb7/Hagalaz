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

export interface DefinitionSummaryDto {
    id: number;
    name?: string;
}

export type * from "./dtos";
import type { UpdateItemDefinitionResultDto, DuplicateItemDefinitionResultDto, UpdateNpcCombatLevelResultDto, UpdateObjectNameResultDto, UpdateVarpBitDefinitionResultDto, UpdateConfigDefinitionResultDto, ReplaceSpriteImageResultDto } from "./dtos";

export type DefinitionMutationResult = UpdateItemDefinitionResultDto | DuplicateItemDefinitionResultDto | UpdateNpcCombatLevelResultDto | UpdateObjectNameResultDto | UpdateVarpBitDefinitionResultDto | UpdateConfigDefinitionResultDto | ReplaceSpriteImageResultDto;

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
