import { Component, inject } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MAT_DIALOG_DATA, MatDialogModule } from "@angular/material/dialog";

export interface CacheWriteReview {
    description: string;
    notes: readonly string[];
    groups: readonly { label: string; changes: readonly string[] }[];
}

@Component({
    selector: "cache-write-dialog",
    imports: [MatDialogModule, MatButtonModule],
    template: `
        <h2 mat-dialog-title>{{ review ? "Review map changes" : "Confirm cache update" }}</h2>
        <mat-dialog-content>
            <p>{{ description }}</p>
            @if (review; as data) {
                @for (note of data.notes; track note) { <p class="supporting-text mt-2">{{ note }}</p> }
                @for (group of data.groups; track group.label) {
                    <h3 class="mt-4 font-semibold">{{ group.label }}</h3>
                    <ul class="ml-5 list-disc">
                        @for (change of group.changes; track $index) { <li>{{ change }}</li> }
                    </ul>
                }
            } @else {
                <p class="supporting-text mt-3">This changes the live cache immediately. Verify the target and values before continuing.</p>
            }
        </mat-dialog-content>
        <mat-dialog-actions align="end">
            <button mat-button [mat-dialog-close]="false" cdkFocusInitial>Cancel</button>
            <button mat-flat-button [mat-dialog-close]="true">{{ review ? "Publish changes" : "Confirm update" }}</button>
        </mat-dialog-actions>`,
})
export class CacheWriteDialogComponent {
    private readonly data = inject<string | CacheWriteReview>(MAT_DIALOG_DATA);
    readonly description = typeof this.data === "string" ? this.data : this.data.description;
    readonly review = typeof this.data === "string" ? null : this.data;
}
