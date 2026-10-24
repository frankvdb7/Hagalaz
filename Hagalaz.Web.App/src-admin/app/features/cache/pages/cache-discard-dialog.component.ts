import { Component } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatDialogModule } from "@angular/material/dialog";

@Component({
    selector: "cache-discard-dialog",
    imports: [MatDialogModule, MatButtonModule],
    template: `<h2 mat-dialog-title>Discard unsaved changes?</h2><mat-dialog-content>Your changes have not been saved. Keep editing or discard them to continue.</mat-dialog-content><mat-dialog-actions align="end"><button mat-button [mat-dialog-close]="false" cdkFocusInitial>Keep editing</button><button mat-flat-button [mat-dialog-close]="true">Discard changes</button></mat-dialog-actions>`,
})
export class CacheDiscardDialogComponent {}
