import { NgTemplateOutlet } from "@angular/common";
import { Component, computed, input, linkedSignal, output, signal } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import type { MapLoadProgress } from "../services/map-render.models";

type MapLoadView = "first-load" | "progress" | "failed" | "empty" | "issues" | "none";

/** What the load is waiting for, earliest stage first. */
export function mapLoadStage({ regionsSettled, regionsTotal, appearancesSettled, appearancesTotal, texturesSettled, texturesTotal }: MapLoadProgress): { label: string; detail: string | null } {
    if (regionsSettled < regionsTotal) return { label: regionsTotal === 1 ? "Loading region" : "Loading regions", detail: regionsTotal > 1 ? `${regionsSettled} of ${regionsTotal}` : null };
    if (appearancesSettled < appearancesTotal) return { label: "Loading objects", detail: `${appearancesSettled} of ${appearancesTotal}` };
    if (texturesSettled < texturesTotal) return { label: "Loading textures", detail: `${texturesSettled} of ${texturesTotal}` };
    return { label: "Finishing scene", detail: null };
}

/** Settled work over the work known so far. */
export function mapLoadPercent(progress: MapLoadProgress): number {
    const total = progress.regionsTotal + progress.appearancesTotal + progress.texturesTotal;
    return total ? Math.floor(((progress.regionsSettled + progress.appearancesSettled + progress.texturesSettled) / total) * 100) : 0;
}

/** Presents the map scene's loading, writing and read-failure states over the viewport. */
@Component({
    selector: "cache-map-load-status",
    imports: [NgTemplateOutlet, MatButtonModule, MatIconModule, MatProgressBarModule, MatProgressSpinnerModule],
    template: `
        <p class="sr-only" role="status">{{ announcement() }}</p>
        @switch (view()) {
            @case ("first-load") {
                <div class="load-card studio-surface">
                    <mat-spinner diameter="36" aria-hidden="true" />
                    <strong>{{ stage().label }}</strong>
                    @if (stage().detail; as detail) { <span class="supporting-text">{{ detail }}</span> }
                </div>
            }
            @case ("progress") {
                <div class="load-pill is-delayed studio-surface">
                    <div class="pill-row">
                        <strong>{{ activity() ?? stage().label }}</strong>
                        @if (!activity() && stage().detail) { <span class="supporting-text">{{ stage().detail }}</span> }
                    </div>
                    <mat-progress-bar [mode]="activity() ? 'indeterminate' : 'determinate'" [value]="percent()" aria-label="Map scene progress" />
                </div>
            }
            @case ("failed") {
                <div class="load-card is-interactive studio-surface">
                    <mat-icon class="is-error" aria-hidden="true">error_outline</mat-icon>
                    <strong>Region could not be loaded</strong>
                    <ng-container [ngTemplateOutlet]="issueList" />
                    <button mat-stroked-button type="button" (click)="retry.emit()"><mat-icon>refresh</mat-icon>Retry</button>
                </div>
            }
            @case ("empty") {
                <div class="load-card is-interactive studio-surface">
                    <mat-icon aria-hidden="true">layers_clear</mat-icon>
                    <strong>No renderable tiles or objects</strong>
                    @if (issues().length) { <ng-container [ngTemplateOutlet]="issueList" /> } @else { <span class="supporting-text">Check the region availability.</span> }
                    <button mat-stroked-button type="button" (click)="retry.emit()"><mat-icon>refresh</mat-icon>Retry</button>
                </div>
            }
            @case ("issues") {
                <div class="load-pill is-interactive studio-surface">
                    <div class="pill-row">
                        <mat-icon class="studio-icon-small is-error" aria-hidden="true">warning_amber</mat-icon>
                        <strong>{{ failureText() }}</strong>
                        <button mat-icon-button type="button" class="pill-close" aria-label="Dismiss load issues" (click)="dismissed.set(true)"><mat-icon>close</mat-icon></button>
                    </div>
                    @if (detailsOpen()) { <ng-container [ngTemplateOutlet]="issueList" /> }
                    <div class="pill-actions">
                        <button mat-button type="button" [attr.aria-expanded]="detailsOpen()" (click)="detailsOpen.set(!detailsOpen())">{{ detailsOpen() ? 'Hide details' : 'Details' }}</button>
                        <button mat-button type="button" (click)="retry.emit()"><mat-icon>refresh</mat-icon>Retry</button>
                    </div>
                </div>
            }
        }
        <ng-template #issueList>
            <ul class="issue-list map-scroll-area">
                @for (issue of issues(); track issue) { <li>{{ issue }}</li> }
            </ul>
        </ng-template>
    `,
    styles: `
        :host { position: absolute; inset: 0; display: grid; place-items: center; padding: 12px; pointer-events: none; }
        .load-card { display: grid; justify-items: center; gap: 8px; max-width: min(28rem, 100%); padding: 20px 24px; text-align: center; font: var(--mat-sys-body-medium); }
        .load-pill { display: grid; align-self: end; justify-self: end; gap: 6px; width: min(18rem, 100%); padding: 8px 12px; font: var(--mat-sys-body-small); }
        .is-interactive { pointer-events: auto; }
        .is-delayed { animation: map-load-reveal 150ms ease-out 250ms both; }
        .is-error { color: var(--mat-sys-error); }
        .pill-row { display: flex; align-items: center; gap: 8px; min-height: 24px; }
        .pill-row strong { flex: 1 1 0; min-width: 0; font-weight: 500; }
        .is-delayed .pill-row { white-space: nowrap; }
        .pill-close { margin-block: -8px; margin-inline-end: -8px; }
        .pill-actions { display: flex; justify-content: flex-end; gap: 4px; }
        .issue-list { display: grid; gap: 4px; width: 100%; max-height: 8rem; margin: 0; padding: 0; overflow: auto; list-style: none; overflow-wrap: anywhere; text-align: start; color: var(--mat-sys-on-surface-variant); }
        @keyframes map-load-reveal { from { opacity: 0; } to { opacity: 1; } }
        @media (prefers-reduced-motion: reduce) { .is-delayed { animation-duration: 1ms; } }
        @media (width <= 600px) { .load-pill { margin-block-end: 52px; } }
    `,
})
export class MapLoadStatusComponent {
    readonly loading = input(false);
    readonly hasRegions = input(false);
    readonly hasGeometry = input(false);
    readonly progress = input.required<MapLoadProgress>();
    readonly issues = input<readonly string[]>([]);
    /** A cache write in progress, described for the user. */
    readonly activity = input<string | null>(null);
    readonly retry = output<void>();

    readonly detailsOpen = signal(false);
    readonly dismissed = linkedSignal(() => { this.progress().failures; return false; });
    readonly stage = computed(() => mapLoadStage(this.progress()));
    readonly failureText = computed(() => {
        const failures = this.progress().failures;
        return `${failures} ${failures === 1 ? "item" : "items"} could not be loaded`;
    });
    readonly view = computed<MapLoadView>(() => {
        if (this.loading() && !this.hasRegions()) return "first-load";
        if (this.loading() || this.activity()) return "progress";
        const failed = this.progress().failures > 0;
        if (!this.hasGeometry()) return failed ? "failed" : "empty";
        return failed && !this.dismissed() ? "issues" : "none";
    });
    /** Totals grow as regions and appearances reveal more work, so keep the highest value within one load. */
    readonly percent = linkedSignal<{ loading: boolean; progress: MapLoadProgress }, number>({
        source: () => ({ loading: this.loading(), progress: this.progress() }),
        computation: ({ loading, progress }, previous) => {
            if (!loading) return 100;
            const sameLoad = previous?.source.loading && progress.regionsSettled >= previous.source.progress.regionsSettled;
            return Math.max(sameLoad ? previous.value : 0, mapLoadPercent(progress));
        },
    });
    /** Only changes of state, so screen readers do not hear each counter update. */
    readonly announcement = computed(() => {
        switch (this.view()) {
            case "first-load": return "Loading map scene.";
            case "progress": return this.activity() ?? "Loading map scene.";
            case "failed": return "Region could not be loaded.";
            case "empty": return "No renderable tiles or objects.";
            case "issues": return `${this.failureText()}.`;
            default: return this.hasGeometry() ? "Map scene ready." : "";
        }
    });
}
