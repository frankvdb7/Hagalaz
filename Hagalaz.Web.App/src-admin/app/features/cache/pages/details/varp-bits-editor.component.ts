import { Component, DestroyRef, inject, linkedSignal } from "@angular/core";
import { FormField, form, min, required } from "@angular/forms/signals";
import { MatButtonModule } from "@angular/material/button";
import { MatDialog } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { firstValueFrom } from "rxjs";
import { CacheVarpBitsStore } from "../../stores/cache-varp-bits.store";
import { CachePendingChangesService } from "../../cache-pending-changes.service";
import { CacheWriteDialogComponent } from "../cache-write-dialog";

@Component({
    selector: "cache-varp-bits-editor",
    imports: [FormField, MatButtonModule, MatFormFieldModule, MatInputModule],
    template: `<form novalidate (submit)="$event.preventDefault(); setVarpBit()"><div class="grid grid-cols-1 gap-x-3 sm:grid-cols-2"><mat-form-field class="w-full" appearance="outline"><mat-label>Config ID</mat-label><input matInput type="number" [formField]="varpMutationForm.configId" /><mat-error>Enter zero or greater.</mat-error></mat-form-field><mat-form-field class="w-full" appearance="outline"><mat-label>Bit length</mat-label><input matInput type="number" [formField]="varpMutationForm.bitLength" /><mat-error>Enter zero or greater.</mat-error></mat-form-field><mat-form-field class="w-full" appearance="outline"><mat-label>Bit offset</mat-label><input matInput type="number" [formField]="varpMutationForm.bitOffset" /><mat-error>Enter zero or greater.</mat-error></mat-form-field></div><button mat-flat-button type="submit" class="mb-5" [disabled]="store.loading() || varpMutationForm().invalid()">Save varp bit</button></form>`,
})
export class VarpBitEditorComponent {
    readonly store = inject(CacheVarpBitsStore);
    private readonly dialog = inject(MatDialog);
    private readonly pendingChanges = inject(CachePendingChangesService);
    private readonly varpMutationModel = linkedSignal(() => {
        const value = (this.store.byIdResult() ?? {}) as Record<string, unknown>;
        return {
            id: this.store.definitionId() ?? 0,
            configId: (value.configId ?? 0) as number,
            bitLength: (value.bitLength ?? 0) as number,
            bitOffset: (value.bitOffset ?? 0) as number,
        };
    });
    readonly varpMutationForm = form(this.varpMutationModel, (path) => {
        required(path.id);
        min(path.id, 0);
        required(path.configId);
        min(path.configId, 0);
        required(path.bitLength);
        min(path.bitLength, 0);
        required(path.bitOffset);
        min(path.bitOffset, 0);
    });

    constructor() {
        this.pendingChanges.register(
            {
                dirty: () => {
                    const v = this.varpMutationModel();
                    const record = (this.store.byIdResult() ?? {}) as Record<string, unknown>;
                    return v.configId !== (record.configId ?? 0) || v.bitLength !== (record.bitLength ?? 0) || v.bitOffset !== (record.bitOffset ?? 0);
                },
                saving: () => this.store.saving(),
            },
            inject(DestroyRef)
        );
    }

    async setVarpBit(): Promise<void> {
        if (this.varpMutationForm().invalid() || this.store.loading()) return;
        const v = this.varpMutationModel();
        if (!(await this.confirmUpdate(`Update varp bit #${v.id}: config ${v.configId}, length ${v.bitLength}, offset ${v.bitOffset}?`))) return;
        this.store.setVarpBit(v);
    }

    private async confirmUpdate(description: string): Promise<boolean> {
        return (await firstValueFrom(this.dialog.open(CacheWriteDialogComponent, { data: description, maxWidth: "32rem" }).afterClosed())) === true;
    }
}
