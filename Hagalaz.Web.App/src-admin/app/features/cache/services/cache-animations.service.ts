import type { GetAnimationDefinitionDto } from "./dtos";
import { Service, type Signal } from "@angular/core";
import { environment } from "../../../../environments/environment";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";

@Service()
export class CacheAnimationsService {
    private readonly baseUrl = `${environment.cacheApiUrl}types/animations`;

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<GetAnimationDefinitionDto>(this.baseUrl, id);
    }

    rangeResource(range: Signal<{ startId: number; endIdExclusive: number } | null>) {
        return cacheQueryResource<GetAnimationDefinitionDto[]>(`${this.baseUrl}/range`, range);
    }
}
