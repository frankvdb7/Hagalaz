import type { ClientMapValueMapEntryDto } from "./client-map-value-map-entry-dto";
import type { DynamicValueDto } from "./dynamic-value-dto";

export interface GetClientMapDefinitionDto {
    id: number;
    defaultStringValue: string;
    defaultIntValue: number;
    keyType: string;
    valueType: string;
    count: number;
    valueMap: ClientMapValueMapEntryDto[] | null;
    values: DynamicValueDto[] | null;
}
