import { CharacterStatType, GetAllCharacterStatisticsResult } from "@app/services/character-statistics/character-statistics.models";
import { CharacterStatisticEntity } from "@app/main/highscores/highscores.models";

export function mapStatisticsResult(result: GetAllCharacterStatisticsResult): [stats: CharacterStatisticEntity[], total: number] {
    return [
        result.results?.map((s, index) => {
            const stat = s.statistics[0];
            return {
                rank: (result.metaData.page - 1) * result.metaData.limit + (index + 1),
                name: s.displayName,
                level: stat.level,
                experience: stat.experience,
            };
        }) || [],
        result.metaData?.total || 0,
    ];
}

export function mapStatisticString(statType: string | undefined): CharacterStatType | null {
    if (!statType) {
        return null;
    }
    const normalized = statType.toLowerCase();
    return CharacterStatType[(normalized.charAt(0).toUpperCase() + normalized.slice(1)) as keyof typeof CharacterStatType];
}
