export interface MultiValueResult<T> {
    results: T[];
}

export interface ValueResult<T> {
    result: T;
}

export interface PagingModel {
    page: number;
    limit: number;
}

export interface PagingMetaDataModel extends PagingModel {
    total: number;
}

export type SortType = "asc" | "desc";

export interface Result {
    succeeded: boolean;
}
