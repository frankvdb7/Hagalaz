import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";

export interface CharacterDto {
    id: number;
    username: string;
    displayName: string;
    email: string;
    createdDate: string;
    lastLoginDate: string | null;
    isBanned: boolean;
    isMuted: boolean;
}

@Injectable({ providedIn: "root" })
export class CharacterService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl.replace('cache/api/v1/cache/', 'api/v1/characters')}`; // Rough guess on the path mapping

    search(query: string): Observable<CharacterDto[]> {
        return this.http.get<CharacterDto[]>(`${this.baseUrl}/search`, {
            params: { query }
        });
    }

    getStats(id: number): Observable<any> {
        return this.http.get<any>(`${this.baseUrl}/stats/${id}`);
    }
}
