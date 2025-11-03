import { computed, inject } from "@angular/core";
import { patchState, signalStore, withComputed, withMethods, withState } from "@ngrx/signals";
import { rxMethod } from "@ngrx/signals/rxjs-interop";
import { pipe, switchMap } from "rxjs";
import { tapResponse } from "@ngrx/operators";
import { EquipmentService } from "@app/services/equipment/equipment.service";
import { EquipmentSlot } from "@app/services/equipment/equipment.models";

export interface EquipmentState {
    loading: boolean;
    error: unknown;
    equipment: EquipmentSlot[];
}

const initialState: EquipmentState = {
    loading: false,
    error: null,
    equipment: [],
};

export const EquipmentStore = signalStore(
    { providedIn: "root" },
    withState(initialState),
    withComputed(({ equipment }) => ({
        equippedItems: computed(() => equipment()),
    })),
    withMethods((store, equipmentService = inject(EquipmentService)) => ({
        loadEquipment: rxMethod<void>(
            pipe(
                switchMap(() =>
                    equipmentService.getEquipment().pipe(
                        tapResponse({
                            next: (result) => patchState(store, { equipment: result }),
                            error: (error) => patchState(store, { error }),
                            finalize: () => patchState(store, { loading: false }),
                        })
                    )
                )
            )
        ),
    }))
);
