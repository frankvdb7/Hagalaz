import type { GetCs2ScriptDto, GetCs2DefinitionDto } from "./dtos";
import { Service } from "@angular/core";
import { CacheTypeService } from "./cache-resource";

@Service()
export class CacheCs2Service extends CacheTypeService<GetCs2ScriptDto, GetCs2DefinitionDto> {
    constructor() {
        super("cs2");
    }
}
