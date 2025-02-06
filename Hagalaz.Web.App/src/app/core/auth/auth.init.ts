import { inject } from "@angular/core";
import { filter, map, Observable, take } from "rxjs";
import { AuthStore } from "./auth.store";
import { toObservable } from "@angular/core/rxjs-interop";

export function initAuthStore(): () => Observable<void> {
    const store = inject(AuthStore);
    const loading$ = toObservable(store.loading);
    const waitForLoading$ = loading$.pipe(
        filter((loading) => !loading),
        take(1)
    );
    return () => {
        store.refreshToken();
        return waitForLoading$.pipe(map(() => void 0));
    };
}
