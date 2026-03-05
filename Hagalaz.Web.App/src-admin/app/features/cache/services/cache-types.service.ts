import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import { ArchiveSizesDto, SearchResultDto, TypeKind, TypeMutationResultDto } from "./cache.models";

@Injectable({ providedIn: "root" })
export class CacheTypesService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl}types/`;

    getArchiveSizes(): Observable<ArchiveSizesDto> {
        return this.http.get<ArchiveSizesDto>(`${this.baseUrl}archive-sizes`);
    }

    getById(kind: TypeKind, id: number): Observable<unknown> {
        return this.http.get<unknown>(`${this.baseUrl}${kind}/${id}`);
    }

    getRange(kind: TypeKind, startId: number, endIdExclusive: number): Observable<unknown[]> {
        return this.http.get<unknown[]>(`${this.baseUrl}${kind}/range`, {
            params: {
                startId,
                endIdExclusive,
            },
        });
    }

    search(kind: "items" | "npcs" | "sprites", query: string, offset: number, limit: number): Observable<SearchResultDto<unknown>> {
        return this.http.get<SearchResultDto<unknown>>(`${this.baseUrl}${kind}/search`, {
            params: {
                query,
                offset,
                limit,
            },
        });
    }

    updateNpcCombatLevel(id: number, combatLevel: number): Observable<TypeMutationResultDto> {
        return this.http.post<TypeMutationResultDto>(`${this.baseUrl}npcs/${id}/combat-level`, { combatLevel });
    }

    updateObjectName(id: number, name: string): Observable<TypeMutationResultDto> {
        return this.http.post<TypeMutationResultDto>(`${this.baseUrl}objects/${id}/name`, { name });
    }

    updateVarpBit(id: number, configId: number, bitLength: number, bitOffset: number): Observable<TypeMutationResultDto> {
        return this.http.post<TypeMutationResultDto>(`${this.baseUrl}varp-bits/${id}`, { configId, bitLength, bitOffset });
    }

    updateConfigDefinition(id: number, defaultValue: number, valueType: string): Observable<TypeMutationResultDto> {
        return this.http.post<TypeMutationResultDto>(`${this.baseUrl}config-definitions/${id}`, { defaultValue, valueType });
    }
}
