import { Component, computed, input, model, output } from "@angular/core";
import { FormsModule, type NgForm } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import type { GetModelDto } from "../services/dtos";

export const modelTransformFields = [
    { key: "scale", label: "Model scale", min: 1, max: 1024 },
    { key: "rotation", label: "Rotation (degrees)", min: -360, max: 360 },
    { key: "offset", label: "Offset", min: -10000, max: 10000 },
] as const;
type TransformKey = (typeof modelTransformFields)[number]["key"];

export function createModelPreviewTransform(): Record<TransformKey, number[]> {
    return { scale: [128, 128, 128], rotation: [0, 0, 0], offset: [0, 0, 0] };
}

@Component({
    selector: "aside[cache-model-properties]",
    imports: [FormsModule, MatButtonModule, MatCheckboxModule, MatFormFieldModule, MatIconModule, MatInputModule],
    templateUrl: "./model-properties.component.html",
    host: { class: "studio-compact-inspector" },
    styleUrl: "./model-properties.component.scss",
})
export class ModelPropertiesComponent {
    readonly value = input.required<GetModelDto>();
    readonly transform = input.required<Record<TransformKey, number[]>>();
    readonly selectedGroup = model(-1);
    readonly transformInput = output<{ key: TransformKey; axis: number; value: number | null }>();
    readonly resetPreview = output<void>();
    readonly axes = ["X", "Y", "Z"];
    readonly transformFields = modelTransformFields;
    readonly skinGroups = computed(() => [...new Set(this.value().vertexSkins ?? [])].filter((id) => id >= 0).sort((a, b) => a - b));
    readonly groupVertices = computed(() => this.value().vertexSkins?.filter((id) => id === this.selectedGroup()).length ?? 0);
    readonly usesAlpha = computed(() => this.value().faceAlpha.some((alpha) => alpha > 0));

    reset(form: NgForm): void {
        const defaults = createModelPreviewTransform();
        form.resetForm(Object.fromEntries(modelTransformFields.flatMap((field) => defaults[field.key].map((value, axis) => [field.key + axis, value]))));
        this.resetPreview.emit();
    }
}
