import { NgTemplateOutlet } from "@angular/common";
import { afterRenderEffect, Component, computed, effect, ElementRef, inject, input, model, output, signal, untracked, viewChild } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatTabsModule } from "@angular/material/tabs";
import { type MatTree, MatTreeModule } from "@angular/material/tree";
import type { MapPlacement, MapRenderRegion } from "../services/map-render.models";
import { buildSceneTree, type ScenePlacement, type SceneSelection, type SceneTreeNode } from "./map-scene-tree";

/** The region's terrain and placements as a tree, with visibility toggles. */
@Component({
    selector: "cache-map-scene-panel",
    imports: [NgTemplateOutlet, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatTabsModule, MatTreeModule],
    template: `
        <nav mat-tab-nav-bar class="scene-tabs map-workspace-tabs" mat-stretch-tabs="false" aria-label="Scene view" animationDuration="0ms" [tabPanel]="scenePanel">
            <a mat-tab-link active><mat-icon class="studio-icon-small mr-2" aria-hidden="true">account_tree</mat-icon>Scene</a>
        </nav>
        <mat-tab-nav-panel #scenePanel class="scene-content">
            <mat-form-field appearance="outline" subscriptSizing="dynamic" class="studio-compact-inspector">
                <mat-label>Filter placements</mat-label>
                <input matInput [value]="query()" (input)="filter($any($event.target).value)" />
            </mat-form-field>
            <mat-tree #tree class="scene-tree map-scene-tree map-scroll-area" [dataSource]="nodes()" [childrenAccessor]="children" [expansionKey]="key">
                <ng-template matTreeNodeDef let-node>
                    <mat-tree-node matTreeNodePadding matTreeNodePaddingIndent="6" [class.is-selected]="isSelected(node)">
                        <span class="toggle-space"></span>
                        <button type="button" class="tree-label" (click)="activate(node)">
                            <mat-icon class="studio-icon-small" aria-hidden="true">{{ node.icon }}</mat-icon><span>{{ node.label }}</span>
                        </button>
                        @if (node.kind !== 'more') { <ng-container [ngTemplateOutlet]="eye" [ngTemplateOutletContext]="{ $implicit: node }" /> }
                    </mat-tree-node>
                </ng-template>
                <ng-template matTreeNodeDef let-node [matTreeNodeDefWhen]="hasChildren">
                    <mat-tree-node matTreeNodePadding matTreeNodePaddingIndent="6" isExpandable [class.is-selected]="isSelected(node)">
                        <button mat-icon-button type="button" class="tree-icon" matTreeNodeToggle [attr.aria-label]="'Toggle ' + node.label">
                            <mat-icon>{{ tree.isExpanded(node) ? 'expand_more' : 'chevron_right' }}</mat-icon>
                        </button>
                        <button type="button" class="tree-label" [disabled]="node.kind !== 'root'" (click)="activate(node)"><mat-icon class="studio-icon-small" aria-hidden="true">{{ node.icon }}</mat-icon><span>{{ node.label }}</span></button>
                        @if (node.kind === 'group') { <ng-container [ngTemplateOutlet]="eye" [ngTemplateOutletContext]="{ $implicit: node }" /> }
                    </mat-tree-node>
                </ng-template>
            </mat-tree>
            <ng-template #eye let-node>
                <button mat-icon-button type="button" class="tree-icon" [attr.aria-label]="(isHidden(node) ? 'Show ' : 'Hide ') + node.label" [attr.aria-pressed]="isHidden(node)" (click)="toggle(node)">
                    <mat-icon>{{ isHidden(node) ? 'visibility_off' : 'visibility' }}</mat-icon>
                </button>
            </ng-template>
            @if (!nodes().length) { <p class="supporting-text">{{ loading() ? 'Loading region data…' : 'No region data loaded.' }}</p> }
        </mat-tab-nav-panel>
    `,
    styles: `
        :host { display: flex; flex: 1 1 0; flex-direction: column; min-width: 0; min-height: 0; }
        .scene-tabs { flex: none; }
        .scene-content { display: flex; flex: 1 1 0; flex-direction: column; gap: 8px; min-height: 0; padding: 12px; }
        .scene-tree { flex: 1 1 0; min-height: 0; margin-inline-end: -12px; padding-inline-end: 2px; overflow: hidden auto; }
        mat-tree-node { box-sizing: border-box; width: 100%; border-radius: 8px; }
        mat-tree-node.is-selected { background: var(--mat-sys-primary-container); color: var(--mat-sys-on-primary-container); }
        .toggle-space { flex: 0 0 22px; }
        .tree-label { display: flex; flex: 1 1 0; align-items: center; gap: 6px; min-width: 0; height: 28px; padding: 0 1px; border: 0; border-radius: 6px; background: none; color: inherit; font: inherit; text-align: start; }
        .tree-label mat-icon { flex: none; }
        .tree-label > span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
        button.tree-label:not(:disabled) { cursor: pointer; }
        button.tree-label:not(:disabled):hover { background: var(--mat-sys-surface-container-high); }
        button.tree-label:focus-visible { outline: 2px solid var(--mat-sys-primary); outline-offset: -2px; }
        .tree-icon { flex: 0 0 auto; color: var(--mat-sys-on-surface-variant); }
    `,
})
export class MapScenePanelComponent {
    readonly loading = input(false);
    readonly regions = input.required<readonly MapRenderRegion[]>();
    readonly placements = input.required<readonly ScenePlacement[]>();
    readonly names = input.required<ReadonlyMap<number, string>>();
    readonly selected = input<MapPlacement>();
    readonly terrainVisible = model(true);
    readonly hiddenGroups = model<ReadonlySet<string>>(new Set());
    readonly hiddenPlacements = model<ReadonlySet<MapPlacement>>(new Set());
    readonly placementActivated = output<MapPlacement>();
    readonly selection = input<SceneSelection | null>(null);
    readonly selectionActivated = output<SceneSelection>();
    readonly query = signal("");
    private readonly limit = signal(50);
    readonly nodes = computed(() => buildSceneTree({ regions: this.regions(), placements: this.placements(), query: this.query(), limit: this.limit(), names: this.names(), selected: this.selected() }));
    readonly children = (node: SceneTreeNode): SceneTreeNode[] => node.children ?? [];
    readonly key = (node: SceneTreeNode): string => node.key;
    readonly hasChildren = (_: number, node: SceneTreeNode): boolean => node.children !== undefined;
    private readonly tree = viewChild<MatTree<SceneTreeNode, string>>("tree");

    constructor() {
        const host = inject<ElementRef<HTMLElement>>(ElementRef);
        afterRenderEffect(() => {
            if (this.selected()) host.nativeElement.querySelector(".is-selected")?.scrollIntoView?.({ block: "nearest" });
        });
        // Opens the region once, every group while filtering, and the group holding a selection.
        effect(() => {
            const tree = this.tree(), root = this.nodes()[0], filtering = this.query().trim() !== "", selected = this.selected();
            if (!tree || !root) return;
            untracked(() => {
                tree.expand(root);
                for (const group of root.children ?? []) if (filtering || (selected && group.group?.shapes.includes(selected.shape))) tree.expand(group);
            });
        });
    }

    filter(query: string): void {
        this.query.set(query);
        this.limit.set(50);
    }

    isSelected(node: SceneTreeNode): boolean {
        if (node.kind === "root" || node.kind === "terrain") return this.selection() === sceneSelectionOf(node);
        return node.placement !== undefined && node.placement.placement === this.selected();
    }

    isHidden(node: SceneTreeNode): boolean {
        if (node.kind === "terrain") return !this.terrainVisible();
        return this.hiddenGroups().has(node.group?.id ?? "") || (node.placement !== undefined && this.hiddenPlacements().has(node.placement.placement));
    }

    toggle(node: SceneTreeNode): void {
        const placement = node.placement?.placement, group = node.group?.id;
        if (node.kind === "terrain") this.terrainVisible.update(visible => !visible);
        else if (placement) this.hiddenPlacements.update(hidden => toggled(hidden, placement));
        else if (group) this.hiddenGroups.update(hidden => toggled(hidden, group));
    }

    activate(node: SceneTreeNode): void {
        if (node.kind === "root" || node.kind === "terrain") this.selectionActivated.emit(sceneSelectionOf(node));
        else if (node.kind === "more") this.limit.update(limit => limit + 50);
        else if (node.placement) this.placementActivated.emit(node.placement.placement);
    }
}

function sceneSelectionOf(node: SceneTreeNode): SceneSelection {
    return node.kind === "terrain" ? "terrain" : "region";
}

function toggled<T>(values: ReadonlySet<T>, value: T): ReadonlySet<T> {
    const next = new Set(values);
    if (!next.delete(value)) next.add(value);
    return next;
}
