import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import type { GetAllCharacterStatisticsRequest, GetAllCharacterStatisticsResult } from "./character-statistics.models";
import type { Observable } from "rxjs";
import { environment } from "@environment/environment";

@Injectable({
    providedIn: "root",
})
export class CharacterStatisticsService {
    private http = inject(HttpClient);

    getAllStatistics(request: GetAllCharacterStatisticsRequest): Observable<GetAllCharacterStatisticsResult> {
        return this.http.post<GetAllCharacterStatisticsResult>(`${environment.characterApiUrl}characters/stats`, request);
    }
}
