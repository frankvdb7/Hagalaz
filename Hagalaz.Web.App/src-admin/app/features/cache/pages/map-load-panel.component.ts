import { DecimalPipe } from "@angular/common";
import { Component, computed, input } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import type { MapLoadProgress } from "../services/map-render.models";

interface MapLoadStep { label: string; settled: number; total: number; percent: number; done: boolean }

/** Every operation of this load that has work, in the order its requests start. They run in parallel. */
function mapLoadSteps(progress: MapLoadProgress): MapLoadStep[] {
    const steps: [string, number, number][] = [
        ["Regions", progress.regionsSettled, progress.regionsTotal > 1 ? progress.regionsTotal : 0],
        ["Objects", progress.appearancesSettled, progress.appearancesTotal],
        ["Animations", progress.animationsSettled, progress.animationsTotal],
        ["Water", progress.waterSettled, progress.waterTotal],
        ["Textures", progress.texturesSettled, progress.texturesTotal],
    ];
    return steps.filter(([, , total]) => total > 0).map(([label, settled, total]) => ({
        label, settled: Math.min(settled, total), total, percent: Math.floor(Math.min(settled / total, 1) * 100), done: settled >= total,
    }));
}

/** The map scene's loading: overall progress and each parallel operation's settled and total work. */
@Component({
    selector: "cache-map-load-panel",
    imports: [DecimalPipe, MatIconModule, MatProgressBarModule],
    template: `
        <header class="panel-header">
            <strong>Loading map scene</strong>
            <span class="supporting-text">{{ summary() }}</span>
        </header>
        <mat-progress-bar mode="determinate" [value]="percent()" aria-label="Map scene progress" />
        <ul class="operations" aria-label="Parallel operations">
            @for (step of steps(); track step.label) {
                <li [class.is-done]="step.done">
                    <mat-icon class="operation-icon" aria-hidden="true">{{ step.done ? "check_circle" : "autorenew" }}</mat-icon>
                    <span class="operation-label">{{ step.label }}</span>
                    <span class="operation-value">{{ step.settled | number }} / {{ step.total | number }}</span>
                    <mat-progress-bar class="operation-bar" mode="determinate" [value]="step.percent" [attr.aria-label]="step.label + ' progress'" />
                </li>
            }
        </ul>
    `,
    styles: `
        :host {
            display: grid; gap: 6px; width: min(15rem, 100%); padding: 8px 12px; box-sizing: border-box;
            font: var(--mat-sys-body-small); color: var(--mat-sys-on-surface);
        }
        .panel-header { display: flex; align-items: baseline; gap: 8px; }
        .panel-header strong { flex: 1 1 0; font-weight: 500; }
        .operations { display: grid; gap: 4px; margin: 0; padding: 4px 0 0; list-style: none; }
        li { display: grid; grid-template-columns: 14px 1fr auto; align-items: center; column-gap: 6px; row-gap: 2px; }
        .operation-icon { width: 14px; height: 14px; font-size: 14px; color: var(--mat-sys-primary); animation: operation-spin 1.2s linear infinite; }
        .operation-value { font-variant-numeric: tabular-nums; color: var(--mat-sys-on-surface-variant); }
        .operation-bar { grid-column: 2 / -1; --mat-progress-bar-track-height: 2px; --mat-progress-bar-active-indicator-height: 2px; }
        .is-done .operation-icon { animation: none; color: var(--portal-emerald); }
        .is-done .operation-value { color: var(--portal-emerald); }
        .is-done mat-progress-bar { --mat-progress-bar-active-indicator-color: var(--portal-emerald); }
        @keyframes operation-spin { to { transform: rotate(360deg); } }
        @media (prefers-reduced-motion: reduce) { .operation-icon { animation: none; } }
    `,
    host: { role: "group", "aria-label": "Map scene loading" },
})
export class MapLoadPanelComponent {
    readonly progress = input.required<MapLoadProgress>();
    /** Overall progress, kept from moving backwards by the caller. */
    readonly percent = input.required<number>();
    readonly steps = computed(() => mapLoadSteps(this.progress()));
    readonly summary = computed(() => {
        const steps = this.steps();
        const done = steps.filter(step => step.done).length;
        return `${done} of ${steps.length} done`;
    });
}
