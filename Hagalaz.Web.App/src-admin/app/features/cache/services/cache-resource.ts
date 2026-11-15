import { httpResource } from "@angular/common/http";
import type { Signal } from "@angular/core";
import { environment } from "../../../../environments/environment";
import type { SearchDefinitionsDto } from "./dtos/search-definitions-dto";

export type CacheIdRange = { startId: number; endIdExclusive: number };

export type CacheSearchPage = { query: string; offset: number; limit: number };

export function cacheDefinitionResource<T>(url: string, id: Signal<number | null>) {
    return httpResource<T>(() => (id() === null ? undefined : `${url}/${id()}`));
}

export function cacheQueryResource<T>(url: string, query: Signal<Record<string, string | number> | null>) {
    return httpResource<T>(() => {
        const params = query();
        return params ? { url, params } : undefined;
    });
}

/** Owns the definition and range endpoints that every `types/{kind}` cache API exposes. */
export abstract class CacheTypeService<TDefinition, TRangeEntry = TDefinition> {
    protected readonly baseUrl: string;

    protected constructor(kind: string) {
        this.baseUrl = `${environment.cacheApiUrl}types/${kind}`;
    }

    definitionResource(id: Signal<number | null>) {
        return cacheDefinitionResource<TDefinition>(this.baseUrl, id);
    }

    rangeResource(range: Signal<CacheIdRange | null>) {
        return cacheQueryResource<TRangeEntry[]>(`${this.baseUrl}/range`, range);
    }
}

/** A cache type API that also exposes paged text search over its range entries. */
export abstract class CacheSearchableTypeService<TDefinition, TRangeEntry = TDefinition> extends CacheTypeService<TDefinition, TRangeEntry> {
    searchResource(search: Signal<CacheSearchPage | null>) {
        return cacheQueryResource<SearchDefinitionsDto<TRangeEntry>>(`${this.baseUrl}/search`, search);
    }
}
