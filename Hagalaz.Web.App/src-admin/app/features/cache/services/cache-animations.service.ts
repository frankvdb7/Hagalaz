import type { GetAnimationDefinitionDto } from "./dtos";
import { Service } from "@angular/core";
import { CacheTypeService } from "./cache-resource";

@Service()
export class CacheAnimationsService extends CacheTypeService<GetAnimationDefinitionDto> {
    constructor() {
        super("animations");
    }
}
