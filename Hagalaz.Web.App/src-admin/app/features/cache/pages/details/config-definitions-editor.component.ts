import { Component, inject, linkedSignal } from "@angular/core";
import { FormField, form, maxLength, min, minLength, required } from "@angular/forms/signals";
import { MatButtonModule } from "@angular/material/button";
import { MatDialog } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { firstValueFrom } from "rxjs";
import { CacheConfigDefinitionsStore } from "../../stores/cache-config-definitions.store";
import { CacheWriteDialogComponent } from "../cache-write-dialog";

@Component({
    selector: "cache-config-definitions-editor",
    imports: [FormField, MatButtonModule, MatFormFieldModule, MatInputModule],
    template: `<form novalidate (submit)="$event.preventDefault(); setConfigDefinition()"><div class="grid grid-cols-1 gap-x-3 sm:grid-cols-2"><mat-form-field class="w-full" appearance="outline"><mat-label>Default value</mat-label><input matInput type="number" [formField]="configMutationForm.defaultValue" /></mat-form-field><mat-form-field class="w-full" appearance="outline"><mat-label>Value type</mat-label><input matInput [formField]="configMutationForm.valueType" /><mat-error>Enter one character.</mat-error></mat-form-field></div><button mat-flat-button type="submit" class="mb-5" [disabled]="store.loading() || configMutationForm().invalid()">Save config definition</button></form>`,
})
export class ConfigDefinitionEditorComponent {
    readonly store = inject(CacheConfigDefinitionsStore);
    private readonly dialog = inject(MatDialog);
    private readonly configMutationModel = linkedSignal(() => {
        const value = (this.store.byIdResult() ?? {}) as Record<string, unknown>;
        return { id: this.store.definitionId() ?? 0, defaultValue: (value.defaultValue ?? 0) as number, valueType: (value.valueType ?? "") as string };
    });
    readonly configMutationForm = form(this.configMutationModel, (path) => {
        required(path.id);
        min(path.id, 0);
        required(path.defaultValue);
        required(path.valueType);
        minLength(path.valueType, 1);
        maxLength(path.valueType, 1);
    });

    async setConfigDefinition(): Promise<void> {
        if (this.configMutationForm().invalid() || this.store.loading()) return;
        const v = this.configMutationModel();
        if (!(await this.confirmUpdate(`Update config #${v.id}: default ${v.defaultValue}, type "${v.valueType}"?`))) return;
        this.store.setConfigDefinition(v);
    }

    private async confirmUpdate(description: string): Promise<boolean> {
        return (await firstValueFrom(this.dialog.open(CacheWriteDialogComponent, { data: description, maxWidth: "32rem" }).afterClosed())) === true;
    }
}
