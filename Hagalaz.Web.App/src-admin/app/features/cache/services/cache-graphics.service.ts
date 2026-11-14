import type { GetGraphicDefinitionDto } from "./dtos";
import { Service } from "@angular/core";
import { CacheTypeService } from "./cache-resource";

@Service()
export class CacheGraphicsService extends CacheTypeService<GetGraphicDefinitionDto> {
    constructor() {
        super("graphics");
    }
}
