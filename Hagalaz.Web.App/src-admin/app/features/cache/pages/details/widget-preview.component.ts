import { Component, computed, effect, type ElementRef, input, output, signal, viewChild } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatSelectModule } from "@angular/material/select";
import { MatToolbarModule } from "@angular/material/toolbar";
import { MatTooltipModule } from "@angular/material/tooltip";
import { CacheWidgetFontsService } from "../../services/cache-widget-fonts.service";
import { WidgetLayerArtComponent } from "./widget-layer-art.component";
import { componentLabel, type WidgetLayer } from "./widget-layout";
import { WidgetLiveModelsService } from "./widget-live-models.service";
import { WidgetModelImages } from "./widget-model.component";

@Component({
    selector: "cache-widget-preview",
    imports: [
        MatButtonModule,
        MatButtonToggleModule,
        MatCardModule,
        MatFormFieldModule,
        MatIconModule,
        MatSelectModule,
        MatToolbarModule,
        MatTooltipModule,
        WidgetLayerArtComponent,
    ],
    providers: [CacheWidgetFontsService, WidgetModelImages, WidgetLiveModelsService],
    templateUrl: "./widget-preview.component.html",
    styleUrl: "./widget-preview.component.scss",
    host: { "[class.expanded]": "expanded()", "(keydown.escape)": "expanded.set(false)", "aria-label": "Widget preview" },
})
export class WidgetPreviewComponent {
    readonly layers = input.required<WidgetLayer[]>();
    readonly viewport = input.required<{ width: number; height: number }>();
    readonly selectedId = input.required<number | null>();
    readonly interactive = input(false);
    readonly busy = input(false);
    readonly message = input<string | null>(null);
    readonly clickTargets = input<ReadonlySet<number>>(new Set());
    readonly interact = output<void>();
    readonly pointerAction = output<{ id: number; event: "click" | "release" | "enter" | "exit"; x: number; y: number }>();

    dispatchPointer(id: number, event: "click" | "release" | "enter" | "exit", pointer: MouseEvent, element: HTMLElement) {
        if (!this.interactive() || this.pan() || !this.clickTargets().has(id)) return;
        if ((event === "click" || event === "release") && pointer.button !== 0) return;
        const bounds = element.getBoundingClientRect();
        const x = Math.floor((pointer.clientX - bounds.left) / this.zoom());
        const y = Math.floor((pointer.clientY - bounds.top) / this.zoom());
        this.pointerAction.emit({ id, event, x, y });
    }
    readonly select = output<number>();
    readonly duplicate = output<void>();
    readonly remove = output<void>();
    readonly resetEdits = output<void>();

    private readonly stage = viewChild<ElementRef<HTMLElement>>("stage");
    private readonly available = signal({ width: 0, height: 0 });
    readonly zoomMode = signal("fit");
    readonly zoom = computed(() => {
        if (this.zoomMode() !== "fit") return Number(this.zoomMode());
        const { width, height } = this.available();
        const viewport = this.viewport();
        return width && height ? Math.min(2, (width * 0.8) / viewport.width, (height * 0.75) / viewport.height) : 1;
    });
    readonly outlines = signal(false);
    readonly pan = signal(false);
    readonly expanded = signal(false);
    readonly toggle = (value: boolean) => !value;
    readonly editable = computed(() => !this.interactive() && this.selectedId() !== null);
    readonly expandControl = computed(() => this.expanded()
        ? { label: "Close expanded preview", icon: "fullscreen_exit" }
        : { label: "Expand preview", icon: "fullscreen" });
    readonly playControl = computed(() => this.interactive() ? { label: "Stop preview", icon: "stop" } : { label: "Play preview", icon: "play_arrow" });
    readonly status = computed(() => this.busy() ? "Running callback…" : this.message());
    /** Hit targets for each drawn layer; while inspecting, the selected layer stays selectable even when hidden. */
    readonly hits = computed(() => {
        const interactive = this.interactive();
        return this.layers()
            .filter(layer => layer.visible || (!interactive && this.selectedId() === layer.component.id))
            .map(layer => (interactive ? this.activationTarget(layer) : this.inspectionTarget(layer)));
    });
    private drag: { x: number; y: number; left: number; top: number } | undefined;

    constructor() {
        effect((cleanup) => {
            const stage = this.stage()?.nativeElement;
            if (!stage || typeof ResizeObserver === "undefined") return;
            const observer = new ResizeObserver(([entry]) => {
                const { width, height } = entry.contentRect;
                this.available.set({ width, height });
            });
            observer.observe(stage);
            cleanup(() => observer.disconnect());
        });
    }

    private inspectionTarget(layer: WidgetLayer) {
        const id = layer.component.id;
        const selected = this.selectedId() === id;
        return {
            id, layer, selected, pressed: selected, bounds: this.outlines(), inspecting: true, passThrough: false, action: false,
            disabled: this.pan(), label: `Select ${componentLabel(layer.component)}`,
        };
    }

    private activationTarget(layer: WidgetLayer) {
        const id = layer.component.id;
        const action = this.clickTargets().has(id);
        return {
            id, layer, selected: false, pressed: null, bounds: false, inspecting: false, action,
            passThrough: !action && !layer.component.blocksClicks,
            disabled: this.pan() || !action, label: `Activate ${componentLabel(layer.component)}`,
        };
    }

    /** Keyboard activation (detail 0) still selects during interactive preview; pointer clicks are handled as script events. */
    clickLayer(id: number, event: MouseEvent) {
        if (!this.interactive() || event.detail === 0) this.select.emit(id);
    }

    reset(viewport: HTMLElement) {
        this.resetEdits.emit();
        this.zoomMode.set("fit");
        viewport.scrollTop = 0;
        viewport.scrollLeft = 0;
    }

    startPan(event: PointerEvent, viewport: HTMLElement) {
        if (!this.pan() || event.button !== 0) return;
        viewport.setPointerCapture(event.pointerId);
        this.drag = { x: event.clientX, y: event.clientY, left: viewport.scrollLeft, top: viewport.scrollTop };
    }

    movePan(event: PointerEvent, viewport: HTMLElement) {
        if (!this.drag) return;
        viewport.scrollLeft = this.drag.left + this.drag.x - event.clientX;
        viewport.scrollTop = this.drag.top + this.drag.y - event.clientY;
    }

    endPan() {
        this.drag = undefined;
    }
}
