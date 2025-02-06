import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { filter, map, take } from "rxjs/operators";
import { AuthStore } from "./auth.store";
import { toObservable } from "@angular/core/rxjs-interop";

export const authCanActivate: CanActivateFn = (next, state) => {
    const store = inject(AuthStore);
    const router = inject(Router);
    const loading$ = toObservable(store.loading);
    const waitForAuthLoading$ = loading$.pipe(
        filter((loading) => !loading),
        take(1)
    );
    return waitForAuthLoading$.pipe(
        map(() => {
            const authenticated = store.authenticated();
            if (!authenticated) {
                return router.createUrlTree(["/"]);
            }
            return true;
        })
    );
};
