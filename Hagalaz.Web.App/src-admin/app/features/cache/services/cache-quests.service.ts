import type { GetQuestDefinitionDto } from "./dtos";
import { Service, type Signal } from "@angular/core";
import { environment } from "../../../../environments/environment";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";

@Service()
export class CacheQuestsService {
    private readonly baseUrl = `${environment.cacheApiUrl}types/quests`;

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<GetQuestDefinitionDto>(this.baseUrl, id);
    }

    rangeResource(range: Signal<{ startId: number; endIdExclusive: number } | null>) {
        return cacheQueryResource<GetQuestDefinitionDto[]>(`${this.baseUrl}/range`, range);
    }
}
