export interface GetItemTypeDto {
    id: number;
    name: string;
    value: number;
    stackable: boolean;
    noted: boolean;
    noteId: number;
    membersOnly: boolean;
    equipSlot: number;
    equipType: number;
    renderAnimationId: number;
    inventoryOptions: (string | null)[];
    groundOptions: (string | null)[];
    noteTemplateId: number;
    lendId: number;
    lendTemplateId: number;
    boughtItemId: number;
    boughtTemplateId: number;
    interfaceModelId: number;
}
