export interface SearchDefinitionsDto<T> {
    query: string;
    totalMatches: number;
    offset: number;
    limit: number;
    items: T[];
}
