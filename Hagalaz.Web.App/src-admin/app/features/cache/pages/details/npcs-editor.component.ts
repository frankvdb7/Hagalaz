import { Component, inject, linkedSignal } from "@angular/core";
import { FormField, form, min, required } from "@angular/forms/signals";
import { MatButtonModule } from "@angular/material/button";
import { MatDialog } from "@angular/material/dialog";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { firstValueFrom } from "rxjs";
import { CacheNpcsStore } from "../../stores/cache-npcs.store";
import { CacheWriteDialogComponent } from "../cache-write-dialog";

@Component({
    selector: "cache-npcs-editor",
    imports: [FormField, MatButtonModule, MatFormFieldModule, MatInputModule],
    template: `<form novalidate (submit)="$event.preventDefault(); setNpcCombatLevel()"><mat-form-field class="w-full" appearance="outline"><mat-label>Combat level</mat-label><input matInput type="number" [formField]="npcMutationForm.combatLevel" /><mat-error>Enter zero or greater.</mat-error></mat-form-field><button mat-flat-button type="submit" class="mb-5" [disabled]="store.loading() || npcMutationForm().invalid()">Save combat level</button></form>`,
})
export class NpcEditorComponent {
    readonly store = inject(CacheNpcsStore);
    private readonly dialog = inject(MatDialog);
    private readonly npcMutationModel = linkedSignal(() => {
        const value = (this.store.byIdResult() ?? {}) as Record<string, unknown>;
        return { id: this.store.definitionId() ?? 0, combatLevel: (value.combatLevel ?? 0) as number };
    });
    readonly npcMutationForm = form(this.npcMutationModel, (path) => {
        required(path.id);
        min(path.id, 0);
        required(path.combatLevel);
        min(path.combatLevel, 0);
    });

    async setNpcCombatLevel(): Promise<void> {
        if (this.npcMutationForm().invalid() || this.store.loading()) return;
        const v = this.npcMutationModel();
        if (!(await this.confirmUpdate(`Set NPC #${v.id} combat level to ${v.combatLevel}?`))) return;
        this.store.setCombatLevel(v);
    }

    private async confirmUpdate(description: string): Promise<boolean> {
        return (await firstValueFrom(this.dialog.open(CacheWriteDialogComponent, { data: description, maxWidth: "32rem" }).afterClosed())) === true;
    }
}
