export interface GetObjectDefinitionDto {
    id: number;
    name: string;
    sizeX: number;
    sizeY: number;
    solid: boolean;
    gateway: boolean;
    clipType: number;
    surroundings: number;
    actions: (string | null)[];
    varpBitFileId: number;
}
