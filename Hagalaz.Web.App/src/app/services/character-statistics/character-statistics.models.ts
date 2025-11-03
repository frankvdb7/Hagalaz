import { MultiValueResult, PagingMetaDataModel, PagingModel, SortType } from "@app/services/models";

export enum CharacterStatType {
    Attack = "Attack",
    Defence = "Defence",
    Strength = "Strength",
    Constitution = "Constitution",
    Range = "Range",
    Prayer = "Prayer",
    Magic = "Magic",
    Cooking = "Cooking",
    Woodcutting = "Woodcutting",
    Fletching = "Fletching",
    Fishing = "Fishing",
    Firemaking = "Firemaking",
    Crafting = "Crafting",
    Smithing = "Smithing",
    Mining = "Mining",
    Herblore = "Herblore",
    Agility = "Agility",
    Thieving = "Thieving",
    Slayer = "Slayer",
    Farming = "Farming",
    Runecrafting = "Runecrafting",
    Construction = "Construction",
    Hunter = "Hunter",
    Summoning = "Summoning",
    Dungeoneering = "Dungeoneering",

    Overall = "Overall", // custom client side
}

export interface CharacterStatisticDetailDto {
    type: CharacterStatType;
    level: number;
    experience: number;
}

export interface CharacterStatisticsDto {
    displayName: string;
    statistics: CharacterStatisticDetailDto[];
}

export interface GetAllCharacterStatisticsRequest {
    sort?: { experience?: SortType };
    filter?: PagingModel & { type: CharacterStatType };
}

export interface GetCharacterStatisticsByDisplayNameRequest {
    displayName: string;
}

export interface GetAllCharacterStatisticsResult extends MultiValueResult<CharacterStatisticsDto> {
    metaData: PagingMetaDataModel;
}
