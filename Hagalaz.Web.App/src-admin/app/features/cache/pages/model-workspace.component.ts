import { Component, computed, input, linkedSignal, model, output, signal, viewChild } from "@angular/core";
import { JsonPipe } from "@angular/common";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTabsModule } from "@angular/material/tabs";
import { MatTooltipModule } from "@angular/material/tooltip";
import { RouterLink } from "@angular/router";
import { ModelViewerComponent } from "../../../../../shared/model-viewer/model-viewer.component";
import type { ModelGeometry, ModelTexture } from "../../../../../shared/model-viewer/model-scene";
import type { GetModelDto } from "../services/dtos";
import { ModelPropertiesComponent, modelTransformFields, createModelPreviewTransform } from "./model-properties.component";
import { ModelTexturesComponent } from "./model-textures.component";

@Component({
    selector: "cache-model-workspace",
    imports: [
        JsonPipe,
        MatButtonModule,
        MatIconModule,
        MatTabsModule,
        MatTooltipModule,
        RouterLink,
        ModelViewerComponent,
        ModelPropertiesComponent,
        ModelTexturesComponent,
    ],
    templateUrl: "./model-workspace.component.html",
    host: { class: "block min-w-0" },
    styleUrl: "./model-workspace.component.scss",
})
export class ModelWorkspaceComponent {
    readonly modelData = input.required<GetModelDto>();
    readonly modelId = input<number | null>(null);
    readonly geometry = input<ModelGeometry | null>(null);
    readonly textureIds = input.required<readonly number[]>();
    readonly textureImages = input.required<ReadonlyMap<number, ModelTexture>>();
    readonly reload = output<void>();
    readonly tab = model(0);
    readonly showGrid = signal(true);
    readonly wireframe = signal(false);
    readonly selectedGroup = linkedSignal(() => {
        this.modelId();
        return -1;
    });
    readonly selectedTexture = linkedSignal(() => {
        this.modelId();
        return -1;
    });
    readonly transform = linkedSignal(() => {
        this.modelId();
        return createModelPreviewTransform();
    });
    readonly viewer = viewChild(ModelViewerComponent);
    readonly inspectionTabs = computed(() => [
        { label: "Model", icon: "view_in_ar" },
        { label: `Textures (${this.textureIds().length})`, icon: "texture" },
        { label: "Recolors", icon: "palette" },
        { label: "Retextures", icon: "swap_horiz" },
        { label: `Animations (${this.modelData()?.animationIds?.length ?? 0})`, icon: "animation" },
        { label: "Metadata", icon: "data_object" },
    ]);
    readonly workspaceGeometry = computed(() => {
        const geometry = this.geometry();
        if (!geometry) return null;
        const { scale, rotation, offset } = this.transform();
        const [rx, ry, rz] = rotation.map((degrees) => (degrees * Math.PI) / 180);
        const [sx, sy, sz] = [Math.sin(rx), Math.sin(ry), Math.sin(rz)];
        const [cx, cy, cz] = [Math.cos(rx), Math.cos(ry), Math.cos(rz)];
        const rotate = (x: number, y: number, z: number): [number, number, number] => {
            const y1 = y * cx - z * sx,
                z1 = y * sx + z * cx;
            const x2 = x * cy + z1 * sy,
                z2 = -x * sy + z1 * cy;
            return [x2 * cz - y1 * sz, x2 * sz + y1 * cz, z2];
        };
        const place = (x: number, y: number, z: number): [number, number, number] => {
            const point = rotate(x * scale[0] / 128, y * scale[1] / 128, z * scale[2] / 128);
            return [point[0] + offset[0], point[1] + offset[1], point[2] + offset[2]];
        };
        const positions: number[] = [];
        for (let i = 0; i < geometry.positions.length; i += 3) {
            positions.push(...place(geometry.positions[i], geometry.positions[i + 1], geometry.positions[i + 2]));
        }
        const effectors = geometry.effectors?.map(effector => {
            const definition = effector.definition;
            const position = place(...effector.position);
            if (!definition) return { ...effector, position };
            const [x, y, z] = rotate(definition.x, -definition.y, definition.z);
            return { ...effector, position, definition: { ...definition, x, y: -y, z } };
        });
        return { ...geometry, positions, effectors };
    });

    updateTransform(key: "scale" | "rotation" | "offset", axis: number, value: number | null): void {
        const field = modelTransformFields.find((field) => field.key === key);
        if (value === null || !Number.isFinite(value) || !field || value < field.min || value > field.max) return;
        this.transform.update((current) => ({ ...current, [key]: current[key].map((entry, index) => (index === axis ? value : entry)) }));
    }

    resetPreview(): void {
        this.transform.set(createModelPreviewTransform());
        this.viewer()?.reset();
    }
}
