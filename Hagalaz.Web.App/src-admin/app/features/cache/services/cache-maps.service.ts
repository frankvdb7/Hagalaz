import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";
import { MapDecodeRequest, MapTypeDto } from "./cache.models";

@Injectable({ providedIn: "root" })
export class CacheMapsService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl}types/maps/`;

    getById(id: number): Observable<MapTypeDto> {
        return this.http.get<MapTypeDto>(`${this.baseUrl}${id}`);
    }

    decode(id: number, request: MapDecodeRequest): Observable<MapTypeDto> {
        return this.http.post<MapTypeDto>(`${this.baseUrl}${id}/decode`, request);
    }
}
