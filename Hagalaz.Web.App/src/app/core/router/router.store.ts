import { assertInInjectionContext, computed, inject, Signal } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { ActivatedRoute, Data, ParamMap, Params, UrlSegment } from "@angular/router";
import { Observable } from "rxjs";

export function routerParams(): Signal<Params> {
    assertInInjectionContext(routerParams);
    const route = inject(ActivatedRoute);
    return toSignal(route.params, { requireSync: true });
}

export function routerQueryParams(): Signal<Params> {
    assertInInjectionContext(routerQueryParams);
    const route = inject(ActivatedRoute);
    return toSignal(route.queryParams, { requireSync: true });
}

export function routerFragment(): Signal<string | null> {
    assertInInjectionContext(routerFragment);
    const route = inject(ActivatedRoute);
    return toSignal(route.fragment, { requireSync: true });
}

export function routerParamMap(): Signal<ParamMap> {
    assertInInjectionContext(routerParamMap);
    const route = inject(ActivatedRoute);
    return toSignal(route.paramMap, { requireSync: true });
}

export function routerQueryParamMap(): Signal<ParamMap> {
    assertInInjectionContext(routerQueryParamMap);
    const route = inject(ActivatedRoute);
    return toSignal(route.queryParamMap, { requireSync: true });
}

export function routerUrl(): Signal<UrlSegment[]> {
    assertInInjectionContext(routerUrl);
    const route = inject(ActivatedRoute);
    return toSignal(route.url, { requireSync: true });
}

export function routerData<T extends Data = Data>(): Signal<T> {
    assertInInjectionContext(routerData);
    const route = inject(ActivatedRoute);
    return toSignal(route.data as Observable<T>, { requireSync: true });
}
