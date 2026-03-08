import { computed, inject } from "@angular/core";
import { HttpClient, HttpHeaders, HttpParams } from "@angular/common/http";
import { pipe, switchMap, tap, catchError, of, EMPTY, firstValueFrom } from "rxjs";
import { signalStore, withState, withComputed, withMethods, patchState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { AuthStorageService } from "./auth.storage.service";
import { AuthToken, UserInfo, ResultDto } from "./auth.models";
import { environment } from "../../../environments/environment";
import { Router } from "@angular/router";

export interface AuthState {
    loading: boolean;
    error: string | null;
    token: AuthToken | null;
    user: UserInfo | null;
}

const initialState: AuthState = {
    loading: false,
    error: null,
    token: null,
    user: null,
};

export const AuthStore = signalStore(
    { providedIn: "root" },
    withState(initialState),
    withComputed(({ token, user }) => ({
        authenticated: computed(() => token() !== null),
        hasCacheScope: computed(() => token()?.scope.split(" ").includes("cache_api") ?? false),
        isSystemAdministrator: computed(() => user()?.role?.includes("SystemAdministrator") ?? false),
        isAuthorizedAdmin: computed(() => {
            const isAuth = token() !== null;
            const hasScope = token()?.scope.split(" ").includes("cache_api") ?? false;
            const isAdmin = user()?.role?.includes("SystemAdministrator") ?? false;
            return isAuth && hasScope && isAdmin;
        }),
    })),
    withMethods((store, http = inject(HttpClient), authStorage = inject(AuthStorageService), router = inject(Router)) => {
        const getTokens = (data: any, grantType: string) => {
            const options = {
                headers: new HttpHeaders({
                    "Content-Type": "application/x-www-form-urlencoded",
                }),
            };

            const bodyData = {
                ...data,
                grant_type: grantType,
                scope: "openid offline_access email profile roles characters_api cache_api",
                client_id: "storm-app",
            };

            const body = Object.entries(bodyData).reduce(
                (params, [key, value]) => params.set(key, value as string),
                new HttpParams()
            );

            return http.post<AuthToken>(`${environment.authApiUrl}connect/token`, body, options);
        };

        const loadUser = () => http.get<UserInfo>(`${environment.authApiUrl}connect/userinfo`).pipe(
            tap(user => patchState(store, { user })),
            catchError(() => {
                patchState(store, { token: null, user: null });
                authStorage.removeRefreshToken();
                return EMPTY;
            })
        );

        return {
            // Needs to be a Promise for provideAppInitializer in main.ts
            async initialize(): Promise<void> {
                const refreshToken = authStorage.getRefreshToken();
                if (!refreshToken) return;

                try {
                    patchState(store, { loading: true });
                    const token = await firstValueFrom(getTokens({ refresh_token: refreshToken }, "refresh_token"));
                    authStorage.setRefreshToken(token.refresh_token);
                    patchState(store, { token });
                    await firstValueFrom(loadUser());
                } catch {
                    authStorage.removeRefreshToken();
                    patchState(store, { token: null, user: null });
                } finally {
                    patchState(store, { loading: false });
                }
            },

            login: rxMethod<{ email: string; password: string; captcha: string }>(
                pipe(
                    tap(() => patchState(store, { loading: true, error: null })),
                    switchMap(({ email, password, captcha }) => 
                        http.post<ResultDto>(`${environment.authApiUrl}captcha/verify`, { token: captcha }).pipe(
                            switchMap(captchaResult => {
                                if (!captchaResult.succeeded) {
                                    patchState(store, { error: "Captcha verification failed.", loading: false });
                                    return EMPTY;
                                }
                                return getTokens({ username: email, password }, "password").pipe(
                                    tap(token => {
                                        authStorage.setRefreshToken(token.refresh_token);
                                        patchState(store, { token });
                                    }),
                                    switchMap(() => loadUser()),
                                    tap(user => {
                                        const hasScope = store.token()?.scope.split(" ").includes("cache_api");
                                        const isAdmin = user.role?.includes("SystemAdministrator");
                                        if (hasScope && isAdmin) {
                                            router.navigate(["/portal/dashboard"]);
                                        } else {
                                            patchState(store, { error: "Lacks required admin permissions.", loading: false });
                                        }
                                    }),
                                    catchError(() => {
                                        patchState(store, { error: "Authentication failed.", loading: false });
                                        return EMPTY;
                                    })
                                );
                            }),
                            tap(() => patchState(store, { loading: false }))
                        )
                    )
                )
            ),

            devLogin: () => {
                if (environment.production) return;

                const token: AuthToken = {
                    access_token: "dev-token",
                    refresh_token: "dev-refresh",
                    id_token: "dev-id-token",
                    expires_in: 3600,
                    token_type: "Bearer",
                    scope: "openid profile email cache_api",
                };

                const user: UserInfo = {
                    sub: "dev-sub",
                    email: "dev@hagalaz.com",
                    username: "dev@hagalaz.com",
                    preferred_username: "DevArchivist",
                    email_verified: true,
                    role: ["SystemAdministrator"],
                };

                authStorage.setRefreshToken(token.refresh_token);
                patchState(store, { token, user });
                router.navigate(["/portal/dashboard"]);
            },

            logout: rxMethod<void>(
                pipe(
                    switchMap(() => http.get(`${environment.authApiUrl}connect/logout`).pipe(
                        catchError(() => of(null)) // Ignore error on logout
                    )),
                    tap(() => {
                        authStorage.removeRefreshToken();
                        patchState(store, { token: null, user: null });
                        router.navigate(["/login"]);
                    })
                )
            )
        };
    })
);
