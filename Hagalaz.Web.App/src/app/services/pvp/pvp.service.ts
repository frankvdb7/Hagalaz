import { Injectable } from "@angular/core";
import { Observable, of } from "rxjs";
import { PvpStats, PvpMatch } from "./pvp.models";

@Injectable({
    providedIn: "root",
})
export class PvpService {
    getPvpStats(): Observable<PvpStats> {
        return of({
            kills: 100,
            deaths: 50,
            killDeathRatio: 2,
        });
    }

    getMatchHistory(): Observable<PvpMatch[]> {
        return of([
            {
                outcome: "win",
                opponent: "Player 2",
                date: new Date().toISOString(),
            },
            {
                outcome: "loss",
                opponent: "Player 3",
                date: new Date().toISOString(),
            },
            {
                outcome: "win",
                opponent: "Player 4",
                date: new Date().toISOString(),
            },
        ]);
    }
}
