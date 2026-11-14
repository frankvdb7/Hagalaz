import { Component, input, model, output } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatSelectModule } from "@angular/material/select";
import { RouterLink } from "@angular/router";
import type { MapObjectRange, MapObjectTransform } from "./map-scene-geometry";
import { shapeLabel } from "./map-scene-tree";

@Component({
    selector: "cache-map-object-inspector",
    imports: [MatButtonModule, MatButtonToggleModule, MatCheckboxModule, MatFormFieldModule, MatIconModule, MatInputModule, MatSelectModule, RouterLink],
    templateUrl: "./map-object-inspector.component.html",
    styleUrl: "./map-object-inspector.component.scss",
})
export class MapObjectInspectorComponent {
    readonly selection = input.required<MapObjectRange>();
    readonly objectName = input<string>();
    readonly objectModels = input.required<readonly { id: number }[]>();
    readonly objectTransform = input.required<MapObjectTransform | null>();
    readonly objectProperties = input.required<readonly { label: string; value: boolean }[] | null>();
    readonly definitionFailed = input.required<boolean>();
    readonly editTool = input.required<string>();
    readonly positionBase = input.required<{ x: number; y: number }>();
    readonly snap = input.required<number>();
    /** Whether the transform can be changed; off while a save writes the draft. */
    readonly editable = input(true);
    readonly positionMode = model.required<"tile" | "world">();
    readonly transformChange = output<{ field: keyof MapObjectTransform; value: number }>();
    readonly clearSelection = output<void>();
    readonly shapeLabel = shapeLabel;
}
