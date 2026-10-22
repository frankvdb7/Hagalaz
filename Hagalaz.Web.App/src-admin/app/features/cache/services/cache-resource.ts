import { httpResource } from "@angular/common/http";
import type { Signal } from "@angular/core";

export function cacheDefinitionResource<T>(url: string, id: Signal<number | null>) {
    return httpResource<T>(() => (id() === null ? undefined : `${url}/${id()}`));
}

export function cacheQueryResource<T>(url: string, query: Signal<Record<string, string | number> | null>) {
    return httpResource<T>(() => {
        const params = query();
        return params ? { url, params } : undefined;
    });
}
