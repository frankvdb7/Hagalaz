import { Component, computed, input, output } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatListModule } from "@angular/material/list";
import { MatTabsModule } from "@angular/material/tabs";
import { componentLabel, type WidgetLayer, widgetType } from "./widget-layout";
import { WidgetSpriteComponent } from "./widget-sprite.component";

/** Hierarchy and asset panel of the widget workspace; the page owns selection, collapse and visibility state. */
@Component({
    selector: "cache-widget-tree",
    imports: [MatButtonModule, MatCardModule, MatIconModule, MatListModule, MatTabsModule, WidgetSpriteComponent],
    templateUrl: "./widget-tree.component.html",
    styleUrls: ["./widget-panels.scss", "./widget-tree.component.scss"],
})
export class WidgetTreeComponent {
    readonly fileId = input.required<number>();
    /** Visible layers in tree order; collapsed branches are already filtered out. */
    readonly layers = input.required<readonly WidgetLayer[]>();
    readonly parents = input.required<ReadonlySet<number>>();
    readonly collapsed = input.required<ReadonlySet<number>>();
    readonly selectedId = input.required<number | null>();
    readonly unreadable = input.required<readonly number[]>();
    readonly spriteAssets = input.required<readonly number[]>();
    readonly select = output<number | null>();
    readonly toggle = output<number>();
    readonly visibility = output<{ id: number; hidden: boolean }>();
    readonly selectSprite = output<number>();

    readonly rows = computed(() => this.layers().map(({ component, depth }) => {
        const expanded = !this.collapsed().has(component.id);
        const label = componentLabel(component);
        const hidden = component.hidden;
        return {
            id: component.id,
            label,
            hidden,
            expanded,
            indent: 12 + depth * 14,
            selected: this.selectedId() === component.id,
            expandable: this.parents().has(component.id),
            expandIcon: expanded ? "expand_more" : "chevron_right",
            icon: widgetType(component.type).icon,
            visibilityLabel: `${hidden ? "Show" : "Hide"} ${label}`,
            visibilityIcon: hidden ? "visibility_off" : "visibility",
        };
    }));
}
