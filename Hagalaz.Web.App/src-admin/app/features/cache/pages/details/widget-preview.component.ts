import { Component, computed, effect, type ElementRef, input, output, signal, viewChild } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import { CacheWidgetFontsService } from "../../services/cache-widget-fonts.service";
import type { WidgetComponent } from "../../services/widget-component";
import { widgetColor, widgetType, type WidgetLayer } from "./widget-layout";
import { WidgetLiveModelsService } from "./widget-live-models.service";
import { WidgetModelComponent, WidgetModelImages } from "./widget-model.component";
import { WidgetSpriteComponent } from "./widget-sprite.component";
import { WidgetTextComponent } from "./widget-text.component";

@Component({
    selector: "cache-widget-preview",
    imports: [MatIconModule, WidgetSpriteComponent, WidgetTextComponent, WidgetModelComponent],
    providers: [CacheWidgetFontsService, WidgetModelImages, WidgetLiveModelsService],
    templateUrl: "./widget-preview.component.html",
    styleUrl: "./widget-preview.component.scss",
    host: { "[class.expanded]": "expanded()", "(keydown.escape)": "expanded.set(false)", "aria-label": "Widget preview" },
})
export class WidgetPreviewComponent {
    readonly layers = input.required<WidgetLayer[]>();
    readonly viewport = input.required<{ width: number; height: number }>();
    readonly selectedId = input.required<number | null>();
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
    readonly color = widgetColor;
    readonly lineLength = Math.hypot;
    readonly lineAngle = Math.atan2;
    readonly toggle = (value: boolean) => !value;
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

    componentLabel(component: WidgetComponent) {
        return `${component.name || widgetType(component.type).label} (${component.id})`;
    }

    spriteTransform(component: WidgetComponent) {
        const rotation = ((component.spriteRotation ?? 0) * 360) / 65536;
        return `rotate(${rotation}deg) scale(${component.spriteFlipX ? -1 : 1}, ${component.spriteFlipY ? -1 : 1})`;
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
