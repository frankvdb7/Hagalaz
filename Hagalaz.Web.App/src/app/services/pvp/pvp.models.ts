export interface PvpStats {
    kills: number;
    deaths: number;
    killDeathRatio: number;
}

export interface PvpMatch {
    outcome: 'win' | 'loss';
    opponent: string;
    date: string;
}
