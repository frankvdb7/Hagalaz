import {Component, input, linkedSignal, output} from "@angular/core";
import {FormsModule} from "@angular/forms";
import {MatButtonModule} from "@angular/material/button";
import {MatFormFieldModule} from "@angular/material/form-field";
import {MatIconModule} from "@angular/material/icon";
import {MatInputModule} from "@angular/material/input";

@Component({
    selector: "cache-model-lookup",
    imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule],
    template: `<form (ngSubmit)="load()" class="flex flex-wrap items-center gap-2">
        <mat-form-field appearance="outline" subscriptSizing="dynamic" class="model-lookup-field"><mat-label>Model ID</mat-label><mat-icon matPrefix>search</mat-icon><input matInput name="modelId" type="number" min="0" step="1" [ngModel]="draftId()" (ngModelChange)="draftId.set($event)" required #modelId="ngModel" /><mat-error>Enter a model ID of zero or greater.</mat-error></mat-form-field>
        <button mat-flat-button type="submit" [disabled]="modelId.invalid"><mat-icon>search</mat-icon>Load model</button>
    </form>`,
    styles: `.model-lookup-field { width: 180px; }`,
})
export class ModelLookupComponent {
    readonly id = input<number | null>(null);
    readonly draftId = linkedSignal(() => this.id() ?? 0);
    readonly selectModel = output<number>();

    load(): void {
        const id = this.draftId();
        if (Number.isSafeInteger(id) && id >= 0) this.selectModel.emit(id);
    }
}
