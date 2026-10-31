import type { UpdateVarpBitDefinitionDto } from "./dtos";
import type { GetVarpBitDefinitionDto } from "./dtos";
import { HttpClient } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import type { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import type { UpdateVarpBitDefinitionResultDto } from "./cache.models";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";

@Service()
export class CacheVarpBitsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl}types/varp-bits`;

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<GetVarpBitDefinitionDto>(this.baseUrl, id);
    }

    rangeResource(range: Signal<{ startId: number; endIdExclusive: number } | null>) {
        return cacheQueryResource<GetVarpBitDefinitionDto[]>(`${this.baseUrl}/range`, range);
    }

    updateVarpBit(id: number, configId: number, bitLength: number, bitOffset: number): Observable<UpdateVarpBitDefinitionResultDto> {
        return this.http.post<UpdateVarpBitDefinitionResultDto>(`${this.baseUrl}/${id}`, { configId, bitLength, bitOffset } satisfies UpdateVarpBitDefinitionDto);
    }
}
