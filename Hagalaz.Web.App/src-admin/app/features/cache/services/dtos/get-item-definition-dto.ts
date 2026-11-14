import type { UpdateItemDefinitionDto } from "./update-item-definition-dto";

/** The stored item definition fields, plus the read-only values the API derives from them. */
export interface GetItemDefinitionDto extends UpdateItemDefinitionDto {
    noted: boolean;
    stackable: boolean;
    hasWearModel: boolean;
    degradeType: number;
    renderAnimationId: number;
    hasDestroyOption: boolean;
}
