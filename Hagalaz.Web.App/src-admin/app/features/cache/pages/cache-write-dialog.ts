import { Component, inject } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MAT_DIALOG_DATA, MatDialogModule } from "@angular/material/dialog";

@Component({
    selector: "cache-write-dialog",
    imports: [MatDialogModule, MatButtonModule],
    template: `<h2 mat-dialog-title>Confirm cache update</h2><mat-dialog-content><p>{{ description }}</p><p class="supporting-text mt-3">This changes the live cache immediately. Verify the target and values before continuing.</p></mat-dialog-content><mat-dialog-actions align="end"><button mat-button [mat-dialog-close]="false" cdkFocusInitial>Cancel</button><button mat-flat-button [mat-dialog-close]="true">Confirm update</button></mat-dialog-actions>`,
})
export class CacheWriteDialogComponent {
    readonly description = inject<string>(MAT_DIALOG_DATA);
}
