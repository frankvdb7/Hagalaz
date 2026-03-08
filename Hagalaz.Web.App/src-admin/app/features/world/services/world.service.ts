import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../../environments/environment";

export interface WorldStatusDto {
    id: number;
    name: string;
    playerCount: number;
    maxPlayers: number;
    isOnline: boolean;
    uptime: string;
    lastBoot: string;
}

@Injectable({ providedIn: "root" })
export class WorldService {
    private readonly http = inject(HttpClient);
    private readonly baseUrl = `${environment.cacheApiUrl.replace('cache/api/v1/cache/', 'api/v1/world')}`;

    getStatus(): Observable<WorldStatusDto[]> {
        return this.http.get<WorldStatusDto[]>(`${this.baseUrl}/status`);
    }
}
