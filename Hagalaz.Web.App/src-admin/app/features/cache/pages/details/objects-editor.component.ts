import { Component, inject, linkedSignal } from "@angular/core";
import { FormField, form, min, required } from "@angular/forms/signals";
import { MatButtonModule } from "@angular/material/button";
import { MatDialog } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { firstValueFrom } from "rxjs";
import { CacheObjectsStore } from "../../stores/cache-objects.store";
import { CacheWriteDialogComponent } from "../cache-write-dialog";

@Component({
    selector: "cache-objects-editor",
    imports: [FormField, MatButtonModule, MatFormFieldModule, MatInputModule],
    template: `<form novalidate (submit)="$event.preventDefault(); setObjectName()"><mat-form-field class="w-full" appearance="outline"><mat-label>Display name</mat-label><input matInput [formField]="objectMutationForm.name" /><mat-error>Enter a name.</mat-error></mat-form-field><button mat-flat-button type="submit" class="mb-5" [disabled]="store.loading() || objectMutationForm().invalid()">Save object name</button></form>`,
})
export class ObjectEditorComponent {
    readonly store = inject(CacheObjectsStore);
    private readonly dialog = inject(MatDialog);
    private readonly objectMutationModel = linkedSignal(() => {
        const value = (this.store.byIdResult() ?? {}) as Record<string, unknown>;
        return { id: this.store.definitionId() ?? 0, name: (value.name ?? "") as string };
    });
    readonly objectMutationForm = form(this.objectMutationModel, (path) => {
        required(path.id);
        min(path.id, 0);
        required(path.name);
    });

    async setObjectName(): Promise<void> {
        if (this.objectMutationForm().invalid() || this.store.loading()) return;
        const v = this.objectMutationModel();
        if (!(await this.confirmUpdate(`Rename object #${v.id} to "${v.name}"?`))) return;
        this.store.setName(v);
    }

    private async confirmUpdate(description: string): Promise<boolean> {
        return (await firstValueFrom(this.dialog.open(CacheWriteDialogComponent, { data: description, maxWidth: "32rem" }).afterClosed())) === true;
    }
}
