import { Component, input, output } from "@angular/core";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatSelectModule } from "@angular/material/select";
import type { WidgetComponent } from "../../services/widget-component";
import { WidgetAppearanceComponent } from "./widget-appearance.component";
import { acceptIntegerInput } from "./integer-input";
import { widgetType } from "./widget-layout";

type GeometryKey = "x" | "y" | "width" | "height";

/** Identity and layout properties of the widget, or of the selected component together with its appearance. */
@Component({
    selector: "cache-widget-properties",
    imports: [MatFormFieldModule, MatIconModule, MatInputModule, MatSelectModule, WidgetAppearanceComponent],
    template: `
        <section class="property-section">
            <h2><mat-icon>view_in_ar</mat-icon>Widget identity</h2>
            <dl class="identity-fields">
                <dt>Widget ID</dt><dd>{{ fileId() }}</dd>
                @if (component(); as entry) {
                    <dt>Component ID</dt><dd>{{ entry.id }}</dd>
                    <dt>Parent ID</dt><dd>{{ entry.parentId }}</dd>
                    <dt>Type</dt><dd>{{ type(entry.type).label }}</dd>
                } @else {
                    <dt>Components</dt><dd>{{ componentCount() }}</dd>
                }
            </dl>
        </section>
        <section class="property-section">
            <h2><mat-icon>crop_free</mat-icon>Layout</h2>
            @if (component(); as entry) {
                <div class="layout-fields">
                    @for (field of geometry; track field.key) {
                        <mat-form-field appearance="outline" subscriptSizing="dynamic">
                            <mat-label>{{ field.label }}</mat-label>
                            <input matInput type="number" [value]="entry[field.key]" step="1" max="65535" [min]="field.min"
                                (change)="changeGeometry(entry, field.key, $event.target)" />
                        </mat-form-field>
                    }
                    <mat-form-field appearance="outline" subscriptSizing="dynamic">
                        <mat-label>Horizontal anchor</mat-label>
                        <mat-select [value]="entry.xMode" (selectionChange)="patch.emit({ xMode: $event.value })">
                            @for (anchor of anchors; track anchor) { <mat-option [value]="$index">{{ anchor }}</mat-option> }
                        </mat-select>
                    </mat-form-field>
                    <mat-form-field appearance="outline" subscriptSizing="dynamic">
                        <mat-label>Vertical anchor</mat-label>
                        <mat-select [value]="entry.yMode" (selectionChange)="patch.emit({ yMode: $event.value })">
                            @for (anchor of anchors; track anchor) { <mat-option [value]="$index">{{ anchor }}</mat-option> }
                        </mat-select>
                    </mat-form-field>
                </div>
            } @else {
                <dl class="identity-fields"><dt>Width</dt><dd>{{ viewport().width }}</dd><dt>Height</dt><dd>{{ viewport().height }}</dd></dl>
            }
        </section>
        @if (component(); as entry) { <cache-widget-appearance [component]="entry" (patch)="patch.emit($event)" /> }
    `,
    styleUrls: ["./widget-panels.scss"],
    styles: `:host { display: block; }`,
})
export class WidgetPropertiesComponent {
    readonly fileId = input.required<number>();
    readonly component = input.required<WidgetComponent | undefined>();
    readonly componentCount = input.required<number>();
    readonly viewport = input.required<{ width: number; height: number }>();
    /** Emits a change to the selected component. */
    readonly patch = output<Partial<WidgetComponent>>();
    readonly type = widgetType;
    readonly geometry = [
        { key: "x", label: "X", min: -32768 },
        { key: "y", label: "Y", min: -32768 },
        { key: "width", label: "Width", min: 0 },
        { key: "height", label: "Height", min: 0 },
    ] as const;
    readonly anchors = ["Start", "Center", "End", "Relative start", "Relative center", "Relative end"];

    changeGeometry(component: WidgetComponent, key: GeometryKey, input: HTMLInputElement) {
        acceptIntegerInput(input, component[key], value => this.patch.emit({ [key]: value }));
    }
}
