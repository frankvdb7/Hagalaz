import { Service, type Signal } from "@angular/core";
import { environment } from "../../../../environments/environment";
import { cacheDefinitionResource } from "./cache-resource";

@Service()
export class CacheMapsService {
    private readonly baseUrl = `${environment.cacheApiUrl}types/maps`;

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<unknown>(this.baseUrl, id);
    }
}
