import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { GetAllCharacterStatisticsRequest, GetAllCharacterStatisticsResult, GetCharacterStatisticsByDisplayNameRequest, CharacterStatisticsDto } from "./character-statistics.models";
import { Observable } from "rxjs";
import { environment } from "@environment/environment";

@Injectable({
    providedIn: "root",
})
export class CharacterStatisticsService {
    private http = inject(HttpClient);

    getAllStatistics(request: GetAllCharacterStatisticsRequest): Observable<GetAllCharacterStatisticsResult> {
        return this.http.post<GetAllCharacterStatisticsResult>(`${environment.characterApiUrl}characters/stats`, request);
    }

    getStatisticsByDisplayName(request: GetCharacterStatisticsByDisplayNameRequest): Observable<CharacterStatisticsDto> {
        return this.http.get<CharacterStatisticsDto>(`${environment.characterApiUrl}characters/${request.displayName}/stats`);
    }
}
