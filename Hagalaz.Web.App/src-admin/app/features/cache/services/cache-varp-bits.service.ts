import type { GetVarpBitDefinitionDto, UpdateVarpBitDefinitionDto } from "./dtos";
import { HttpClient } from "@angular/common/http";
import { inject, Service } from "@angular/core";
import type { Observable } from "rxjs";
import type { UpdateVarpBitDefinitionResultDto } from "./cache.models";
import { CacheTypeService } from "./cache-resource";

@Service()
export class CacheVarpBitsService extends CacheTypeService<GetVarpBitDefinitionDto> {
    private readonly http = inject(HttpClient);

    constructor() {
        super("varp-bits");
    }

    updateVarpBit(id: number, configId: number, bitLength: number, bitOffset: number): Observable<UpdateVarpBitDefinitionResultDto> {
        return this.http.post<UpdateVarpBitDefinitionResultDto>(`${this.baseUrl}/${id}`, { configId, bitLength, bitOffset } satisfies UpdateVarpBitDefinitionDto);
    }
}
