import { computed, inject } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { ActivatedRoute } from "@angular/router";

/** Shared ID validation for cache detail routes; zero is a valid definition ID. */
export function injectCacheDefinitionId() {
    const route = inject(ActivatedRoute);
    const params = toSignal(route.paramMap);
    return computed(() => {
        const raw = params()?.get("id") ?? route.snapshot.paramMap.get("id") ?? "";
        const id = Number(raw);
        return /^\d+$/.test(raw) && Number.isSafeInteger(id) ? id : null;
    });
}
