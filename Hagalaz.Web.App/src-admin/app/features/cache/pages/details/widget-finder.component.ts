import { Component, computed, input, output, signal } from "@angular/core";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatListModule } from "@angular/material/list";
import { MatSelectModule } from "@angular/material/select";
import type { WidgetComponent } from "../../services/widget-component";
import { componentLabel, widgetType } from "./widget-layout";

/** Searches the widget's components by label and type; the search terms are local to this panel. */
@Component({
    selector: "cache-widget-finder",
    imports: [MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatListModule, MatSelectModule],
    template: `
        <mat-card appearance="outlined" class="workspace-panel" aria-label="Find components">
            <div class="catalog-filters">
                <mat-form-field appearance="outline" subscriptSizing="dynamic">
                    <mat-label>Search components</mat-label>
                    <mat-icon matPrefix>search</mat-icon>
                    <input matInput type="search" [value]="filter()" (input)="filter.set($event.target.value)" />
                </mat-form-field>
                <mat-form-field appearance="outline" subscriptSizing="dynamic">
                    <mat-label>Component type</mat-label>
                    <mat-select [value]="typeFilter()" (selectionChange)="typeFilter.set($event.value)">
                        <mat-option value="">All</mat-option>
                        @for (option of typeOptions(); track option.value) { <mat-option [value]="option.value">{{ option.label }}</mat-option> }
                    </mat-select>
                </mat-form-field>
            </div>
            <mat-action-list class="panel-list" aria-label="Matching components">
                @for (entry of matching(); track entry.id) {
                    <button mat-list-item type="button" [activated]="entry.selected" [attr.aria-pressed]="entry.selected" (click)="select.emit(entry.id)">
                        <mat-icon matListItemIcon>{{ entry.icon }}</mat-icon>
                        <span matListItemTitle>{{ entry.label }}</span>
                    </button>
                } @empty { <p class="panel-message">No matching components.</p> }
            </mat-action-list>
        </mat-card>
    `,
    styleUrls: ["./widget-panels.scss"],
    styles: `
        :host { display: contents; }
        .catalog-filters { display: grid; gap: 8px; padding: 12px 12px 0; }
    `,
})
export class WidgetFinderComponent {
    readonly components = input.required<readonly WidgetComponent[]>();
    readonly selectedId = input.required<number | null>();
    readonly select = output<number>();
    readonly filter = signal("");
    readonly typeFilter = signal("");

    readonly typeOptions = computed(() => [...new Set(this.components().map(({ type }) => type))]
        .sort((a, b) => a - b)
        .map(type => ({ value: type.toString(), label: widgetType(type).label })));

    readonly matching = computed(() => {
        const type = this.typeFilter();
        const query = this.filter().trim().toLowerCase();
        return this.components()
            .filter(component => !type || component.type === Number(type))
            .map(component => ({
                id: component.id,
                label: componentLabel(component),
                icon: widgetType(component.type).icon,
                selected: this.selectedId() === component.id,
            }))
            .filter(entry => entry.label.toLowerCase().includes(query));
    });
}
