import { afterNextRender, Component, computed, DestroyRef, effect, ElementRef, inject, Injector, linkedSignal, signal, untracked } from "@angular/core";
import { takeUntilDestroyed, toSignal } from "@angular/core/rxjs-interop";
import type { Subscription } from "rxjs";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatCheckboxModule } from "@angular/material/checkbox";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatListModule } from "@angular/material/list";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatSelectModule } from "@angular/material/select";
import { MatTabsModule } from "@angular/material/tabs";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { CacheSpritesService } from "../../services/cache-sprites.service";
import { CacheWidgetsService } from "../../services/cache-widgets.service";
import { pointerEvents, WidgetRuntime } from "../../services/widget-runtime";
import { decodeWidgetComponent, type WidgetComponent } from "../../services/widget-component";
import { injectCacheDefinitionId } from "../cache-definition-id";
import { layoutWidget, widgetType } from "./widget-layout";
import { WidgetPreviewComponent } from "./widget-preview.component";
import { WidgetSpriteComponent } from "./widget-sprite.component";

@Component({
    selector: "cache-widgets-detail-page",
    imports: [
        MatButtonModule,
        MatCardModule,
        MatCheckboxModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatListModule,
        MatProgressBarModule,
        MatSelectModule,
        MatTabsModule,
        RouterLink,
        WidgetPreviewComponent,
        WidgetSpriteComponent,
    ],
    providers: [CacheSpritesService],
    templateUrl: "./widgets-detail.page.html",
    styleUrl: "./widgets-detail.page.scss",
})
export class WidgetsDetailPageComponent {
    readonly optionIsSet = Boolean;
    private readonly destroyRef = inject(DestroyRef);
    private previewRequest?: Subscription;
    private runtime?: WidgetRuntime;
    private readonly mouseEnterTargets = signal<ReadonlyMap<number, boolean>>(new Map());
    private readonly mouseExitTargets = signal<ReadonlyMap<number, boolean>>(new Map());
    readonly interactive = signal(false);
    readonly previewBusy = signal(false);
    readonly previewMessage = signal<string | null>(null);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly injector = inject(Injector);
    private readonly route = inject(ActivatedRoute);
    private readonly queryParams = toSignal(this.route.queryParamMap, { initialValue: this.route.snapshot.queryParamMap });
    readonly widgetId = injectCacheDefinitionId();
    readonly requestedComponent = computed(() => {
        const id = this.queryParams().get("component");
        return id && /^\d+$/.test(id) && Number(id) <= 65535 ? Number(id) : null;
    });
    readonly cache = inject(CacheWidgetsService);
    readonly widget = computed(() => this.cache.widgets().find((widget) => widget.fileId === this.widgetId()));
    readonly archive = this.cache.archive(() => this.widget()?.fileId);
    private readonly decoded = computed(() => {
        const components: WidgetComponent[] = [];
        const unreadable: number[] = [];
        if (!this.archive.hasValue()) return { components, unreadable };
        const slots = new Map(this.archive.value().entries.map((entry) => [entry.subFileId, entry]));
        const children = [...(this.widget()?.children ?? [])].sort((a, b) => a.childId - b.childId);
        for (const child of children) {
            try {
                const entry = slots.get(child.index);
                if (entry === undefined) unreadable.push(child.childId);
                else components.push(decodeWidgetComponent(child.childId, entry.dataBase64));
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
        const id = this.requestedComponent();
        return id !== null && this.decoded().components.some((entry) => entry.id === id) ? id : null;
    });
    readonly componentLinkMessage = computed(() => {
        const id = this.requestedComponent();
        if (id === null || !this.archive.hasValue() || this.decoded().components.some((entry) => entry.id === id)) return null;
        return this.unreadable().includes(id) ? `Could not read component ${id}.` : `Component ${id} not found.`;
    });
    readonly selected = computed(() => this.components().find(({ id }) => id === this.selectedId()));
    readonly selectedCallbacks = computed(() => {
        if (!this.archive.hasValue()) return null;
        const slot = this.widget()?.children.find((child) => child.childId === this.selectedId())?.index;
        return this.archive.value().entries.find((entry) => entry.subFileId === slot)?.callbacks ?? null;
    });
    private readonly pointerCallbacks = computed(() => {
        const slots = new Map((this.archive.hasValue() ? this.archive.value().entries : []).map(entry => [entry.subFileId, entry.callbacks]));
        return new Map(this.widget()?.children.map(child => {
            const events = new Set(slots.get(child.index)?.filter(callback => callback.scriptId !== null).map(callback => callback.event));
            const mouseEnter = this.mouseEnterTargets().get(child.childId);
            if (mouseEnter === true) events.add(pointerEvents.enter);
            if (mouseEnter === false) events.delete(pointerEvents.enter);
            const mouseExit = this.mouseExitTargets().get(child.childId);
            if (mouseExit === true) events.add(pointerEvents.exit);
            if (mouseExit === false) events.delete(pointerEvents.exit);
            return [child.childId, events] as const;
        }));
    });
    readonly clickTargets = computed(() => new Set([...this.pointerCallbacks()]
        .filter(([, callbacks]) => Object.values(pointerEvents).some(event => callbacks.has(event)))
        .map(([id]) => id)));
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
        this.requestedComponent();
        return new Set<number>();
    });
    readonly tree = computed(() => this.layers().filter((layer) => layer.ancestors.every((id) => !this.collapsed().has(id))));
    readonly parents = computed(() => new Set(this.components().map(({ parentId }) => parentId)));
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

    constructor() {
        effect(() => {
            this.widgetId();
            untracked(() => this.stopPreview());
        });
    }

    startPreview() {
        const id = this.widgetId();
        if (id === null) return;
        this.reset();
        this.interactive.set(true);
        this.previewBusy.set(true);
        this.previewRequest = this.cache.loadProgram(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: bundle => {
                if (id !== this.widgetId()) return;
                this.previewBusy.set(false);
                try {
                    const baseline = this.decoded().components;
                    const { width, height } = this.viewport();
                    this.runtime = new WidgetRuntime(id, bundle, (component, changes, children) =>
                        layoutWidget([...baseline.map(entry => ({ ...entry, ...changes.get(entry.id) })), ...children], width, height)
                            .find(layer => layer.component.id === component));
                    this.runPreview("open");
                } catch (error) {
                    this.previewMessage.set((error as Error).message);
                }
            },
            error: () => {
                this.previewBusy.set(false);
                this.previewMessage.set("Could not load widget scripts.");
            },
        });
    }

    runPreview(event: "open" | "click" | "release" | "enter" | "exit" | "manual", component?: number, callback?: string, trigger?: string, x?: number, y?: number) {
        if (!this.runtime || !this.interactive()) return;
        if (event !== "open" && event !== "manual" && (component === undefined ||
            !this.pointerCallbacks().get(component)?.has(pointerEvents[event]))) return;
        const { state, message } = this.runtime.run({ event, component, callback, trigger: trigger ? Number(trigger) : undefined, x, y });
        this.previewMessage.set(message);
        if (state) {
            this.mouseEnterTargets.set(this.runtime.hoverTargets(pointerEvents.enter));
            this.mouseExitTargets.set(this.runtime.hoverTargets(pointerEvents.exit));
            const changes = new Map(state.components.map(entry => [entry.id, entry]));
            this.components.set([...this.decoded().components.map(entry => ({ ...entry, ...changes.get(entry.id) })), ...(state.dynamicChildren ?? [])]);
        }
    }

    private stopPreview() {
        this.previewRequest?.unsubscribe();
        this.runtime = undefined;
        this.mouseEnterTargets.set(new Map());
        this.mouseExitTargets.set(new Map());
        this.interactive.set(false);
        this.previewBusy.set(false);
        this.previewMessage.set(null);
    }

    componentLabel(component: WidgetComponent) {
        return `${component.name || widgetType(component.type).label} (${component.id})`;
    }

    select(id: number | null) {
        this.selectedId.set(id);
        const layer = this.layers().find((entry) => entry.component.id === id);
        if (layer) this.collapsed.update((current) => new Set([...current].filter((ancestor) => !layer.ancestors.includes(ancestor))));
    }

    selectFromPreview(id: number) {
        if (this.interactive()) {
            this.runPreview("click", id);
            return;
        }
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
        this.stopPreview();
        this.components.set(this.decoded().components);
        this.select(null);
    }
}
