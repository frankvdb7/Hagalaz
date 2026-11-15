import type { GetConfigDefinitionDto, UpdateConfigDefinitionDto } from "./dtos";
import { HttpClient } from "@angular/common/http";
import { inject, Service } from "@angular/core";
import type { Observable } from "rxjs";
import type { UpdateConfigDefinitionResultDto } from "./cache.models";
import { CacheTypeService } from "./cache-resource";

@Service()
export class CacheConfigDefinitionsService extends CacheTypeService<GetConfigDefinitionDto> {
    private readonly http = inject(HttpClient);

    constructor() {
        super("config-definitions");
    }

    updateConfigDefinition(id: number, defaultValue: number, valueType: string): Observable<UpdateConfigDefinitionResultDto> {
        return this.http.post<UpdateConfigDefinitionResultDto>(`${this.baseUrl}/${id}`, { defaultValue, valueType } satisfies UpdateConfigDefinitionDto);
    }
}
