import { computed, effect, inject } from "@angular/core";
import { UserInfo } from "@app/services/user/user.model";
import { patchState, signalStore, withComputed, withHooks, withMethods, withState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { pipe, switchMap, timeout } from "rxjs";
import { AuthStore } from "../auth/auth.store";
import { UserService } from "@app/services/user/user.service";
import { tapResponse } from "@ngrx/operators";

export interface UserState {
    loading: boolean;
    error: unknown;
    info: UserInfo | null;
}

const initialState: UserState = {
    loading: false,
    error: null,
    info: null,
};

export const UserStore = signalStore(
    { providedIn: "root" },
    withState(initialState),
    withComputed(({ info }) => ({
        username: computed(() => info()?.username || "Unknown"),
        displayName: computed(() => info()?.preferred_username || "Unknown"),
    })),
    withMethods((store, userService = inject(UserService)) => ({
        loadUser: rxMethod<void>(
            pipe(
                switchMap(() =>
                    userService.getUserInfo().pipe(
                        tapResponse({
                            next: (result) => patchState(store, { info: result }),
                            error: (error) => patchState(store, { error }),
                            finalize: () => patchState(store, { loading: false }),
                        }),
                        timeout(2500)
                    )
                )
            )
        ),
    })),
    withHooks({
        onInit: (store, authStore = inject(AuthStore)) => {
            effect(() => {
                if (authStore.authenticated()) {
                    store.loadUser();
                }
            });
        },
    })
);
