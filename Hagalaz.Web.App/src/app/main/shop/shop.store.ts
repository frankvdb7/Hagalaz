import { computed, inject } from "@angular/core";
import { patchState, signalStore, withComputed, withMethods, withState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { pipe, switchMap } from "rxjs";
import { tapResponse } from "@ngrx/operators";
import { ShopService } from "@app/services/shop/shop.service";
import { CosmeticItem } from "@app/services/shop/shop.models";
import { UserStore } from "@app/core/user/user.store";

export interface ShopState {
    loading: boolean;
    error: unknown;
    items: CosmeticItem[];
}

const initialState: ShopState = {
    loading: false,
    error: null,
    items: [],
};

export const ShopStore = signalStore(
    { providedIn: "root" },
    withState(initialState),
    withComputed(({ items }) => ({
        shopItems: computed(() => items()),
    })),
    withMethods((store, shopService = inject(ShopService), userStore = inject(UserStore)) => ({
        loadShopItems: rxMethod<void>(
            pipe(
                switchMap(() =>
                    shopService.getShopItems().pipe(
                        tapResponse({
                            next: (result) => patchState(store, { items: result }),
                            error: (error) => patchState(store, { error }),
                            finalize: () => patchState(store, { loading: false }),
                        })
                    )
                )
            )
        ),
        purchaseItem: (item: CosmeticItem) => {
            if (userStore.hagalazCoins() >= item.price) {
                // Deduct coins (mocked)
                const newBalance = userStore.hagalazCoins() - item.price;
                // In a real application, you would call a service to update the user's balance on the backend
                // For now, we'll just log it and update the local store (this is not persistent)
                console.log(`Purchased ${item.name} for ${item.price} Hagalaz Coins. New balance: ${newBalance}`);
                // Mock updating the user's balance in the store
                // This would typically be handled by a successful API response
                userStore.setHagalazCoins(newBalance);
                // Grant item (mocked)
                console.log(`Item ${item.name} granted to player.`);
            } else {
                console.log(`Insufficient Hagalaz Coins to purchase ${item.name}. Needed: ${item.price}, Have: ${userStore.hagalazCoins()}`);
                // Optionally, display a user-friendly error message
            }
        },
    }))
);
