import type { UpdateObjectNameDto } from "./dtos";
import type { GetObjectDefinitionDto } from "./dtos";
import { HttpClient } from "@angular/common/http";
import { inject, Service, type Signal } from "@angular/core";
import type { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import type { UpdateObjectNameResultDto } from "./cache.models";
import { cacheDefinitionResource, cacheQueryResource } from "./cache-resource";

@Service()
export class CacheObjectsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl}types/objects`;

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<GetObjectDefinitionDto>(this.baseUrl, id);
    }

    rangeResource(range: Signal<{ startId: number; endIdExclusive: number } | null>) {
        return cacheQueryResource<GetObjectDefinitionDto[]>(`${this.baseUrl}/range`, range);
    }

    updateObjectName(id: number, name: string): Observable<UpdateObjectNameResultDto> {
        return this.http.post<UpdateObjectNameResultDto>(`${this.baseUrl}/${id}/name`, { name } satisfies UpdateObjectNameDto);
    }
}
