import { Component, computed, input, model } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatSelectModule } from "@angular/material/select";
import { MatTabsModule } from "@angular/material/tabs";
import { MatTooltipModule } from "@angular/material/tooltip";
import type { MapFloor } from "../services/map-render.models";
import { MapFloorPickerComponent } from "./map-floor-picker.component";
import type { TerrainBrush, TerrainBrushTool } from "./map-terrain-edit";

type BrushNumberField = "plane" | "size" | "strength" | "overlayShape" | "overlayRotation";
interface BrushField { key: BrushNumberField; label: string; options: readonly { value: number; label: string }[]; }

const fields: Record<BrushNumberField, BrushField> = {
    plane: { key: "plane", label: "Plane", options: [0, 1, 2, 3].map(value => ({ value, label: `Plane ${value}` })) },
    size: { key: "size", label: "Size", options: [1, 3, 5, 7].map(value => ({ value, label: `${value} × ${value}` })) },
    strength: { key: "strength", label: "Strength", options: [2, 4, 8].map(value => ({ value, label: `${value * 32} units` })) },
    overlayShape: { key: "overlayShape", label: "Shape", options: Array.from({ length: 12 }, (_, value) => ({ value, label: value ? `Shape ${value}` : "Full tile" })) },
    overlayRotation: { key: "overlayRotation", label: "Rotation", options: [0, 1, 2, 3].map(value => ({ value, label: `${value * 90}°` })) },
};

/** Brush settings for the map editor's terrain tools. */
@Component({
    selector: "cache-map-terrain-panel",
    imports: [MapFloorPickerComponent, MatButtonModule, MatFormFieldModule, MatIconModule, MatSelectModule, MatTabsModule, MatTooltipModule],
    template: `
        <nav mat-tab-nav-bar class="map-workspace-tabs" mat-stretch-tabs="false" aria-label="Terrain tools" animationDuration="0ms" [tabPanel]="terrainPanel">
            <a mat-tab-link active><mat-icon class="studio-icon-small mr-2" aria-hidden="true">landscape</mat-icon>Terrain</a>
        </nav>
        <mat-tab-nav-panel #terrainPanel>
            <section aria-label="Terrain brush">
                <div class="terrain-tools map-edit-toolbar" role="group" aria-label="Terrain tool">
                    @for (tool of tools; track tool.id) {
                        <button mat-icon-button type="button" [class.is-active]="brush().tool === tool.id" [attr.aria-label]="tool.label" [matTooltip]="tool.label" [attr.aria-pressed]="brush().tool === tool.id" (click)="update({ tool: tool.id })"><mat-icon>{{ tool.icon }}</mat-icon></button>
                    }
                </div>
                <div class="terrain-fields studio-compact-inspector">
                    @for (field of visibleFields(); track field.key) {
                        <mat-form-field appearance="outline" subscriptSizing="dynamic">
                            <mat-label>{{ field.label }}</mat-label>
                            <mat-select [value]="brush()[field.key]" (selectionChange)="setField(field.key, $event.value)">
                                @for (option of field.options; track option.value) { <mat-option [value]="option.value">{{ option.label }}</mat-option> }
                            </mat-select>
                        </mat-form-field>
                    }
                </div>
            </section>
            @if (paintsFloor()) {
                <section aria-label="Floor">
                    <cache-map-floor-picker [floors]="floors()" [overlay]="paintsOverlay()" [chosen]="paintsOverlay() ? brush().overlay : brush().underlay" [loading]="loading()" [error]="error()" (chosenChange)="chooseFloor($event)" />
                </section>
            }
            <p class="supporting-text terrain-hint">Drag on the ground to apply the brush. Each stroke is one undo step.</p>
        </mat-tab-nav-panel>
    `,
    styles: `
        :host { display: block; }
        section { padding: 16px; }
        section + section { border-top: 1px solid var(--mat-sys-outline-variant); }
        .terrain-tools { display: flex; justify-content: space-between; }
        .terrain-tools .is-active { border-radius: 8px; color: var(--mat-sys-primary); outline: 1px solid currentColor; outline-offset: -1px; }
        .terrain-fields { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; margin-top: 12px; }
        .terrain-hint { margin: 0; padding: 0 16px 16px; }
    `,
})
export class MapTerrainPanelComponent {
    readonly brush = model.required<TerrainBrush>();
    readonly floors = input<readonly MapFloor[]>([]);
    readonly loading = input(false);
    readonly error = input<string | null>(null);
    readonly tools: readonly { id: TerrainBrushTool; label: string; icon: string }[] = [
        { id: "raise", label: "Raise", icon: "arrow_upward" },
        { id: "lower", label: "Lower", icon: "arrow_downward" },
        { id: "flatten", label: "Flatten", icon: "horizontal_rule" },
        { id: "underlay", label: "Paint underlay", icon: "format_paint" },
        { id: "overlay", label: "Paint overlay", icon: "layers" },
        { id: "clear-overlay", label: "Clear overlay", icon: "ink_eraser" },
    ];
    readonly paintsOverlay = computed(() => this.brush().tool === "overlay");
    readonly paintsFloor = computed(() => this.paintsOverlay() || this.brush().tool === "underlay");
    readonly visibleFields = computed(() => {
        const tool = this.brush().tool;
        return [fields.plane, fields.size, ...tool === "raise" || tool === "lower" ? [fields.strength] : [], ...tool === "overlay" ? [fields.overlayShape, fields.overlayRotation] : []];
    });

    update(change: Partial<TerrainBrush>): void {
        this.brush.update(brush => ({ ...brush, ...change }));
    }

    setField(key: BrushNumberField, value: number): void {
        this.update({ [key]: value });
    }

    chooseFloor(id: number): void {
        this.update(this.paintsOverlay() ? { overlay: id } : { underlay: id });
    }
}
