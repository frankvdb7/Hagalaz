import type { GetNpcDefinitionDto, UpdateNpcCombatLevelDto } from "./dtos";
import { HttpClient } from "@angular/common/http";
import { inject, Service } from "@angular/core";
import type { Observable } from "rxjs";
import type { UpdateNpcCombatLevelResultDto } from "./cache.models";
import { CacheSearchableTypeService } from "./cache-resource";

@Service()
export class CacheNpcsService extends CacheSearchableTypeService<GetNpcDefinitionDto> {
    private readonly http = inject(HttpClient);

    constructor() {
        super("npcs");
    }

    updateNpcCombatLevel(id: number, combatLevel: number): Observable<UpdateNpcCombatLevelResultDto> {
        return this.http.post<UpdateNpcCombatLevelResultDto>(`${this.baseUrl}/${id}/combat-level`, { combatLevel } satisfies UpdateNpcCombatLevelDto);
    }
}
