import { ChangeDetectionStrategy, Component, inject, signal } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { CommonModule } from "@angular/common";
import { MatButtonModule } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatIconModule } from "@angular/material/icon";
import { MatTabsModule } from "@angular/material/tabs";
import { MatTooltipModule } from "@angular/material/tooltip";
import { MatDividerModule } from "@angular/material/divider";
import { CacheMapsStore } from "../services/cache-maps.store";
import { RunicCardComponent } from "../../../core/components/runic-card/runic-card.component";
import { JsonPropertyViewerComponent } from "../../../core/components/json-viewer/json-viewer.component";

@Component({
    standalone: true,
    selector: "admin-maps-manager",
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        MatProgressBarModule,
        MatProgressSpinnerModule,
        MatIconModule,
        MatTabsModule,
        MatTooltipModule,
        MatDividerModule,
        RunicCardComponent,
        JsonPropertyViewerComponent,
    ],
    templateUrl: "./maps-manager.page.html",
    styleUrl: "./maps-manager.page.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapsManagerPageComponent {
    private readonly fb = inject(FormBuilder);
    readonly store = inject(CacheMapsStore);

    readonly mapLookupForm = this.fb.nonNullable.group({
        id: [0, [Validators.required, Validators.min(0)]]
    });

    readonly xteaForm = this.fb.nonNullable.group({
        key1: [0],
        key2: [0],
        key3: [0],
        key4: [0]
    });

    async loadMap(): Promise<void> {
        const { id } = this.mapLookupForm.getRawValue();
        this.store.loadMap(id);
    }

    async decodeMap(): Promise<void> {
        const { id } = this.mapLookupForm.getRawValue();
        const { key1, key2, key3, key4 } = this.xteaForm.getRawValue();
        this.store.decodeMap({
            id,
            request: { xteaKeys: [key1, key2, key3, key4] }
        });
    }

    clear(): void {
        this.store.clearMap();
        this.mapLookupForm.reset();
        this.xteaForm.reset();
    }
}
