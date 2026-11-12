import { afterNextRender, Component, computed, ElementRef, inject, Injector, linkedSignal, signal } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { CacheSpritesService } from "../../services/cache-sprites.service";
import { CacheWidgetsService } from "../../services/cache-widgets.service";
import { decodeWidgetComponent, type WidgetComponent } from "../../services/widget-component";
import { layoutWidget, widgetType } from "./widget-layout";
import { WidgetPreviewComponent } from "./widget-preview.component";
import { WidgetSpriteComponent } from "./widget-sprite.component";

@Component({
    selector: "cache-widgets-detail-page",
    imports: [MatButtonModule, MatCardModule, MatIconModule, MatProgressBarModule, RouterLink, WidgetPreviewComponent, WidgetSpriteComponent],
    providers: [CacheSpritesService],
    templateUrl: "./widgets-detail.page.html",
    styleUrl: "./widgets-detail.page.scss",
})
export class WidgetsDetailPageComponent {
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly injector = inject(Injector);
    private readonly route = inject(ActivatedRoute);
    private readonly params = toSignal(this.route.paramMap, { initialValue: this.route.snapshot.paramMap });
    readonly cache = inject(CacheWidgetsService);
    readonly widget = computed(() => {
        const id = this.params().get("id");
        return id && /^\d+$/.test(id) ? this.cache.widgets().find((widget) => widget.fileId === Number(id)) : undefined;
    });
    readonly archive = this.cache.archive(() => this.widget()?.fileId);
    private readonly decoded = computed(() => {
        const components: WidgetComponent[] = [];
        const unreadable: number[] = [];
        if (!this.archive.hasValue()) return { components, unreadable };
        const slots = new Map(this.archive.value().entries.map((entry) => [entry.subFileId, entry.dataBase64]));
        const children = [...(this.widget()?.children ?? [])].sort((a, b) => a.childId - b.childId);
        for (const child of children) {
            try {
                const bytes = slots.get(child.index);
                if (bytes === undefined) unreadable.push(child.childId);
                else components.push(decodeWidgetComponent(child.childId, bytes));
            } catch {
                unreadable.push(child.childId);
            }
        }
        return { components, unreadable };
    });
    readonly unreadable = computed(() => this.decoded().unreadable);
    readonly components = linkedSignal(() => this.decoded().components);
    readonly selectedId = linkedSignal(() => {
        this.widget();
        return null as number | null;
    });
    readonly selected = computed(() => this.components().find(({ id }) => id === this.selectedId()));
    readonly viewport = computed(() => {
        const roots = this.decoded().components.filter(({ type, parentId }) => type === 0 && parentId === -1);
        return {
            width: Math.max(512, ...roots.filter(({ widthMode }) => widthMode === 0).map(({ width }) => width)),
            height: Math.max(334, ...roots.filter(({ heightMode }) => heightMode === 0).map(({ height }) => height)),
        };
    });
    readonly layers = computed(() => layoutWidget(this.components(), this.viewport().width, this.viewport().height));
    readonly collapsed = linkedSignal(() => {
        this.widget();
        return new Set<number>();
    });
    readonly tree = computed(() => this.layers().filter((layer) => layer.ancestors.every((id) => !this.collapsed().has(id))));
    readonly parents = computed(() => new Set(this.components().map(({ parentId }) => parentId)));
    readonly sourcePanel = signal<"hierarchy" | "assets">("hierarchy");
    readonly inspector = signal<"properties" | "logic">("properties");
    readonly filter = signal("");
    readonly typeFilter = signal("");
    readonly componentTypes = computed(() => [...new Set(this.components().map(({ type }) => type))].sort((a, b) => a - b));
    readonly matching = computed(() =>
        this.components().filter(
            (component) =>
                (!this.typeFilter() || component.type === Number(this.typeFilter())) &&
                this.componentLabel(component).toLowerCase().includes(this.filter().trim().toLowerCase())
        )
    );
    readonly spriteAssets = computed(() => [
        ...new Set(this.components().flatMap(({ spriteId }) => (spriteId !== undefined && spriteId >= 0 ? [spriteId] : []))),
    ]);
    readonly type = widgetType;
    readonly geometry = [
        { key: "x", label: "X" },
        { key: "y", label: "Y" },
        { key: "width", label: "Width" },
        { key: "height", label: "Height" },
    ] as const;
    readonly cameraFields = [
        { key: "pitch", label: "Pitch" },
        { key: "yaw", label: "Yaw" },
        { key: "roll", label: "Roll" },
        { key: "zoom", label: "Camera distance" },
        { key: "x", label: "Camera X" },
        { key: "y", label: "Camera Y" },
    ] as const;
    readonly anchors = ["Start", "Center", "End", "Relative start", "Relative center", "Relative end"];

    componentLabel(component: WidgetComponent) {
        return `${component.name || widgetType(component.type).label} (${component.id})`;
    }

    select(id: number | null) {
        this.selectedId.set(id);
        const layer = this.layers().find((entry) => entry.component.id === id);
        if (layer) this.collapsed.update((current) => new Set([...current].filter((ancestor) => !layer.ancestors.includes(ancestor))));
    }

    selectFromPreview(id: number) {
        this.select(id);
        afterNextRender(
            () => {
                const row = this.host.nativeElement.querySelector<HTMLElement>(`[data-tree-component="${id}"]`);
                row?.scrollIntoView?.({ block: "nearest" });
            },
            { injector: this.injector }
        );
    }

    toggleBranch(id: number) {
        this.collapsed.update((current) => {
            const next = new Set(current);
            if (!next.delete(id)) next.add(id);
            return next;
        });
    }

    patch(id: number, change: Partial<WidgetComponent>) {
        this.components.update((components) => components.map((component) => (component.id === id ? { ...component, ...change } : component)));
    }

    changeGeometry(component: WidgetComponent, key: "x" | "y" | "width" | "height", input: HTMLInputElement) {
        if (Number.isInteger(input.valueAsNumber) && input.validity.valid) this.patch(component.id, { [key]: input.valueAsNumber });
        else input.value = String(component[key]);
    }

    changeCamera(component: WidgetComponent, key: (typeof this.cameraFields)[number]["key"], input: HTMLInputElement) {
        const camera = component.modelCamera;
        if (!camera) return;
        if (Number.isInteger(input.valueAsNumber) && input.validity.valid) this.patch(component.id, { modelCamera: { ...camera, [key]: input.valueAsNumber } });
        else input.value = String(camera[key]);
    }

    selectSprite(id: number) {
        const component = this.components().find(({ spriteId }) => spriteId === id);
        if (component) this.select(component.id);
    }

    duplicate() {
        const component = this.selected();
        if (!component) return;
        const id = Math.max(-1, ...this.components().map((entry) => entry.id)) + 1;
        this.components.update((components) => [...components, { ...component, id }]);
        this.select(id);
    }

    remove() {
        const id = this.selectedId();
        if (id === null) return;
        const removed = new Set(
            this.layers()
                .filter((layer) => layer.component.id === id || layer.ancestors.includes(id))
                .map((layer) => layer.component.id)
        );
        this.components.update((components) => components.filter((entry) => !removed.has(entry.id)));
        this.select(null);
    }

    reset() {
        this.components.set(this.decoded().components);
        this.select(null);
    }
}
