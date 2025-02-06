import { patchState, signalStore, withHooks, withMethods, withState } from "@ngrx/signals";
import { inject } from "@angular/core";
import { AuthService } from "@app/services/auth/auth.service";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { EMPTY, catchError, debounceTime, exhaustMap, filter, iif, interval, map, mergeMap, of, pipe, switchMap, tap, throwError, timeout } from "rxjs";
import { tapResponse } from "@ngrx/operators";
import { Router } from "@angular/router";
import { takeUntilDestroyed, toObservable } from "@angular/core/rxjs-interop";
import { AuthStorageService } from "@app/services/auth/auth.storage.service";
import { AuthToken } from "@app/services/auth/auth.models";

export interface AuthState {
    authenticated: boolean;
    token: AuthToken | null;
    error: any;
    loading: boolean;
}

const initialState: AuthState = {
    authenticated: false,
    token: null,
    error: null,
    loading: false,
};

export const AuthStore = signalStore(
    { providedIn: "root" },
    withState(initialState),
    withMethods((store, authService = inject(AuthService), authStorageService = inject(AuthStorageService), router = inject(Router)) => ({
        login: rxMethod<{ captcha: string; email: string; password: string }>(
            pipe(
                tap(() => patchState(store, { authenticated: false, error: null, token: null, loading: true })),
                debounceTime(100),
                exhaustMap((params) =>
                    authService.verifyCaptcha(params.captcha).pipe(
                        mergeMap((result) =>
                            iif(
                                () => result.succeeded,
                                of({ ...params, succeeded: result.succeeded }),
                                throwError(() => new Error("Invalid captcha token"))
                            )
                        ),
                        timeout(5000)
                    )
                ),
                exhaustMap((verifyResult) =>
                    authService.login({ email: verifyResult.email, password: verifyResult.password }).pipe(
                        tapResponse({
                            next: async (response) => {
                                authStorageService.setRefreshToken(response.refresh_token);
                                patchState(store, { authenticated: true, token: response });
                                await router.navigate([""]);
                            },
                            error: (error) => {
                                authStorageService.removeRefreshToken();
                                patchState(store, { error });
                            },
                            finalize: () => patchState(store, { loading: false }),
                        }),
                        timeout(5000)
                    )
                ),
                catchError((error) => {
                    patchState(store, { error, loading: false });
                    return EMPTY;
                })
            )
        ),
        logout: rxMethod<void>(
            pipe(
                exhaustMap(() =>
                    authService.logout().pipe(
                        tapResponse({
                            next: async () => {
                                authStorageService.removeRefreshToken();
                                await router.navigate([""]);
                            },
                            error: () => {},
                            finalize: () => patchState(store, initialState),
                        }),
                        timeout(2500)
                    )
                )
            )
        ),
        refreshToken: rxMethod<void>(
            pipe(
                tap(() => patchState(store, { loading: true })),
                map(() => authStorageService.getRefreshToken()),
                exhaustMap((token) => {
                    if (!token) {
                        patchState(store, { loading: false });
                        return EMPTY;
                    }
                    return authService.refreshToken(token!).pipe(
                        tapResponse({
                            next: (response) => {
                                authStorageService.setRefreshToken(response.refresh_token);
                                patchState(store, { authenticated: true, token: response });
                            },
                            error: (error) => patchState(store, { error }),
                            finalize: () => patchState(store, { loading: false }),
                        }),
                        timeout(5000)
                    );
                })
            )
        ),
    })),
    withHooks({
        onInit(store) {
            const token$ = toObservable(store.token);
            token$
                .pipe(
                    filter((token) => !!token),
                    switchMap((token) => interval((token!.expires_in / 2) * 1000)),
                    takeUntilDestroyed()
                )
                .subscribe(() => store.refreshToken());
        },
    })
);
