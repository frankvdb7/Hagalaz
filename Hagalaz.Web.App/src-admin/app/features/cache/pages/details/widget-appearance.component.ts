import { Component, computed, input, output } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { RouterLink } from "@angular/router";
import type { WidgetComponent } from "../../services/widget-component";
import { acceptIntegerInput } from "./integer-input";

type Camera = NonNullable<WidgetComponent["modelCamera"]>;

/** Appearance and behavior of one selected widget component. */
@Component({
    selector: "cache-widget-appearance",
    imports: [MatButtonModule, MatCheckboxModule, MatFormFieldModule, MatIconModule, MatInputModule, RouterLink],
    template: `
        <section class="property-section">
            <h2><mat-icon>brush</mat-icon>Appearance</h2>
            <mat-checkbox [checked]="!component().hidden" (change)="patch.emit({ hidden: !$event.checked })">Visible in preview</mat-checkbox>
            @if (component().type === 4) {
                <mat-form-field appearance="outline" subscriptSizing="dynamic" class="text-field">
                    <mat-label>Text</mat-label>
                    <input matInput [value]="component().text" (input)="patch.emit({ text: $event.target.value })" />
                </mat-form-field>
                <dl class="identity-fields"><dt>Font ID</dt><dd>{{ component().fontId }}</dd></dl>
            }
            @if (component().spriteId !== undefined) {
                <dl class="identity-fields"><dt>Sprite ID</dt><dd>{{ component().spriteId }}</dd></dl>
            }
            @if (modelLink(); as model) {
                <dl class="identity-fields"><dt>Model ID</dt><dd>
                    @if (model.linked) { <a mat-button [routerLink]="['/cache/models', model.id]">{{ model.id }}</a> } @else { {{ model.id }} }
                </dd></dl>
            }
            @if (component().modelCamera; as camera) {
                <div class="layout-fields">
                    @for (field of cameraFields; track field.key) {
                        <mat-form-field appearance="outline" subscriptSizing="dynamic">
                            <mat-label>{{ field.label }}</mat-label>
                            <input matInput type="number" [value]="camera[field.key]" step="1" [min]="field.min" [max]="field.max"
                                (input)="changeCamera(camera, field.key, $event.target)" />
                        </mat-form-field>
                    }
                </div>
            }
        </section>
        <section class="property-section">
            <h2><mat-icon>bolt</mat-icon>Behavior</h2>
            <mat-checkbox [checked]="component().blocksClicks" disabled>Intercepts clicks</mat-checkbox>
        </section>
    `,
    styleUrls: ["./widget-panels.scss"],
    styles: `
        :host { display: block; }
        .text-field { display: flex; }
    `,
})
export class WidgetAppearanceComponent {
    readonly component = input.required<WidgetComponent>();
    readonly patch = output<Partial<WidgetComponent>>();
    /** The referenced model; negative IDs mark an unset model slot and are not linked. */
    readonly modelLink = computed(() => {
        const id = this.component().modelId;
        return id === undefined ? null : { id, linked: id >= 0 };
    });
    readonly cameraFields = [
        { key: "pitch", label: "Pitch", min: 0, max: 65535 },
        { key: "yaw", label: "Yaw", min: 0, max: 65535 },
        { key: "roll", label: "Roll", min: 0, max: 65535 },
        { key: "zoom", label: "Camera distance", min: 1, max: 65535 },
        { key: "x", label: "Camera X", min: -32768, max: 32767 },
        { key: "y", label: "Camera Y", min: -32768, max: 32767 },
    ] as const;

    changeCamera(camera: Camera, key: (typeof this.cameraFields)[number]["key"], input: HTMLInputElement) {
        acceptIntegerInput(input, camera[key], value => this.patch.emit({ modelCamera: { ...camera, [key]: value } }));
    }
}
