import type { GetClientMapDefinitionDto } from "./dtos";
import { Service } from "@angular/core";
import { CacheTypeService } from "./cache-resource";

@Service()
export class CacheClientMapDefinitionsService extends CacheTypeService<GetClientMapDefinitionDto> {
    constructor() {
        super("client-map-definitions");
    }
}
