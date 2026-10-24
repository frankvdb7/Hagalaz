import { HttpClient } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import type { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import type { TypeMutationResultDto } from "./cache.models";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";

@Service()
export class CacheConfigDefinitionsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl}types/config-definitions`;

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<unknown>(this.baseUrl, id);
    }

    rangeResource(range: Signal<{ startId: number; endIdExclusive: number } | null>) {
        return cacheQueryResource<unknown[]>(`${this.baseUrl}/range`, range);
    }

    updateConfigDefinition(id: number, defaultValue: number, valueType: string): Observable<TypeMutationResultDto> {
        return this.http.post<TypeMutationResultDto>(`${this.baseUrl}/${id}`, { defaultValue, valueType });
    }
}
