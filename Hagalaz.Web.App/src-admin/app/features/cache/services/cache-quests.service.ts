import type { GetQuestDefinitionDto } from "./dtos";
import { Service } from "@angular/core";
import { CacheTypeService } from "./cache-resource";

@Service()
export class CacheQuestsService extends CacheTypeService<GetQuestDefinitionDto> {
    constructor() {
        super("quests");
    }
}
