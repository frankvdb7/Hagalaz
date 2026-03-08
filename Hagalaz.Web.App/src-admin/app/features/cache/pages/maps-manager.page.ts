import { ChangeDetectionStrategy, Component, inject, signal, OnInit } from "@angular/core";
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
import { MapTypeDto } from "../services/cache.models";
import { CacheMapsStore } from "../services/cache-maps.store";
import { RunicCardComponent } from "../../../core/components/runic-card/runic-card.component";
import { JsonPropertyViewerComponent } from "../../../core/components/json-viewer/json-viewer.component";
import { MapViewer3dComponent } from "../components/map-viewer-3d/map-viewer-3d.component";

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
        MapViewer3dComponent,
    ],
    templateUrl: "./maps-manager.page.html",
    styleUrl: "./maps-manager.page.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapsManagerPageComponent implements OnInit {
    private readonly fb = inject(FormBuilder);
    readonly store = inject(CacheMapsStore);

    readonly xteaForm = this.fb.nonNullable.group({
        key1: [0],
        key2: [0],
        key3: [0],
        key4: [0]
    });

    ngOnInit(): void {
        this.store.loadInitial();
    }

    onScroll(event: Event): void {
        const element = event.target as HTMLElement;
        const atBottom = element.scrollHeight - element.scrollTop <= element.clientHeight + 100;
        if (atBottom) {
            this.store.loadMore();
        }
    }

    async selectMap(map: MapTypeDto): Promise<void> {
        this.store.loadMap(map.id);
    }

    async decodeMap(): Promise<void> {
        const mapId = this.store.mapInfo()?.id;
        if (!mapId) return;

        const { key1, key2, key3, key4 } = this.xteaForm.getRawValue();
        this.store.decodeMap({
            id: mapId,
            request: { xteaKeys: [key1, key2, key3, key4] }
        });
    }

    clear(): void {
        this.store.clearMap();
        this.xteaForm.reset();
    }
}
