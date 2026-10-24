import { HttpClient } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import type { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import type { TypeMutationResultDto } from "./cache.models";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";

@Service()
export class CacheVarpBitsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl}types/varp-bits`;

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<unknown>(this.baseUrl, id);
    }

    rangeResource(range: Signal<{ startId: number; endIdExclusive: number } | null>) {
        return cacheQueryResource<unknown[]>(`${this.baseUrl}/range`, range);
    }

    updateVarpBit(id: number, configId: number, bitLength: number, bitOffset: number): Observable<TypeMutationResultDto> {
        return this.http.post<TypeMutationResultDto>(`${this.baseUrl}/${id}`, { configId, bitLength, bitOffset });
    }
}
