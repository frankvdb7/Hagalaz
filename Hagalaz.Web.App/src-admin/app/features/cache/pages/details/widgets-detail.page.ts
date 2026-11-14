import { afterNextRender, Component, computed, DestroyRef, effect, ElementRef, inject, Injector, linkedSignal, signal, untracked } from "@angular/core";
import { takeUntilDestroyed, toSignal } from "@angular/core/rxjs-interop";
import type { Subscription } from "rxjs";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatTabsModule } from "@angular/material/tabs";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { CacheSpritesService } from "../../services/cache-sprites.service";
import { CacheWidgetsService, type WidgetPreviewState } from "../../services/cache-widgets.service";
import { pointerEvents, type PreviewEvent, WidgetRuntime } from "../../services/widget-runtime";
import { decodeWidgetComponent, type WidgetComponent } from "../../services/widget-component";
import { injectCacheDefinitionId } from "../cache-definition-id";
import { WidgetFinderComponent } from "./widget-finder.component";
import { layoutWidget } from "./widget-layout";
import { WidgetLogicComponent } from "./widget-logic.component";
import { WidgetPreviewComponent } from "./widget-preview.component";
import { WidgetPropertiesComponent } from "./widget-properties.component";
import { WidgetTreeComponent } from "./widget-tree.component";

@Component({
    selector: "cache-widgets-detail-page",
    imports: [
        MatButtonModule,
        MatCardModule,
        MatIconModule,
        MatProgressBarModule,
        MatTabsModule,
        RouterLink,
        WidgetFinderComponent,
        WidgetLogicComponent,
        WidgetPreviewComponent,
        WidgetPropertiesComponent,
        WidgetTreeComponent,
    ],
    providers: [CacheSpritesService],
    templateUrl: "./widgets-detail.page.html",
    styleUrls: ["./widget-panels.scss", "./widgets-detail.page.scss"],
})
export class WidgetsDetailPageComponent {
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
    readonly loading = computed(() => this.cache.referenceTable.isLoading() || this.archive.isLoading());
    readonly failed = computed(() => Boolean(this.cache.referenceTable.error() || this.archive.error()));
    readonly missingMessage = computed(() => {
        const id = this.widgetId();
        return id === null ? "Enter a valid widget ID." : `Widget ${id} not found.`;
    });
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
    readonly spriteAssets = computed(() => [
        ...new Set(this.components().flatMap(({ spriteId }) => (spriteId !== undefined && spriteId >= 0 ? [spriteId] : []))),
    ]);

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

    retry() {
        if (this.cache.referenceTable.error()) this.cache.referenceTable.reload();
        else this.archive.reload();
    }

    togglePreview() {
        if (this.interactive()) this.reset();
        else this.startPreview();
    }

    runPreview(event: PreviewEvent["event"], component?: number, callback?: string, trigger?: string, x?: number, y?: number) {
        const runtime = this.runtime;
        if (!runtime || !this.interactive() || !this.accepts(event, component)) return;
        const { state, message } = runtime.run({ event, component, callback, trigger: trigger ? Number(trigger) : undefined, x, y });
        this.previewMessage.set(message);
        if (state) this.applyPreviewState(runtime, state);
    }

    /** Pointer events only reach components that currently handle them; open and manual runs are always accepted. */
    private accepts(event: PreviewEvent["event"], component: number | undefined) {
        if (event === "open" || event === "manual") return true;
        return component !== undefined && this.pointerCallbacks().get(component)?.has(pointerEvents[event]) === true;
    }

    private applyPreviewState(runtime: WidgetRuntime, state: WidgetPreviewState) {
        this.mouseEnterTargets.set(runtime.hoverTargets(pointerEvents.enter));
        this.mouseExitTargets.set(runtime.hoverTargets(pointerEvents.exit));
        const changes = new Map(state.components.map(entry => [entry.id, entry]));
        this.components.set([...this.decoded().components.map(entry => ({ ...entry, ...changes.get(entry.id) })), ...(state.dynamicChildren ?? [])]);
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

    patchSelected(change: Partial<WidgetComponent>) {
        const id = this.selectedId();
        if (id !== null) this.patch(id, change);
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
