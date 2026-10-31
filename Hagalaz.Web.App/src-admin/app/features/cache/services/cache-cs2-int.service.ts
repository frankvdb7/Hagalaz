import type { GetCs2IntDefinitionDto } from "./dtos";
import { Service, type Signal } from "@angular/core";
import { environment } from "../../../../environments/environment";
import { cacheDefinitionResource } from "./cache-resource";

@Service()
export class CacheCs2IntService {
    private readonly baseUrl = `${environment.cacheApiUrl}types/cs2-int`;

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<GetCs2IntDefinitionDto>(this.baseUrl, id);
    }
}
