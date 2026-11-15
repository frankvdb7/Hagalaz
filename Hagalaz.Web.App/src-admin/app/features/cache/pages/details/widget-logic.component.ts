import { Component, computed, input, output } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import type { WidgetCallback } from "../../services/cache-widgets.service";
import type { WidgetComponent } from "../../services/widget-component";
import { WidgetCallbackComponent } from "./widget-callback.component";

/** Context-menu actions and cached CS2 callbacks of one selected widget component. */
@Component({
    selector: "cache-widget-logic",
    imports: [MatIconModule, WidgetCallbackComponent],
    template: `
        <section class="property-section">
            <h2><mat-icon>account_tree</mat-icon>Component actions</h2>
            <dl class="identity-fields">
                <dt>Content type</dt><dd>{{ component().contentType }}</dd>
                @if (component().action) { <dt>Action</dt><dd>{{ component().action }}</dd> }
            </dl>
            @if (options().length) { <ul>@for (option of options(); track $index) { <li>{{ option }}</li> }</ul> }
            @else { <p class="panel-message">No context-menu actions.</p> }
        </section>
        <section class="property-section" aria-label="CS2 callbacks">
            <h2><mat-icon>code</mat-icon>CS2 callbacks</h2>
            @if (callbacks(); as entries) {
                @for (callback of entries; track callback.event) {
                    <dl class="identity-fields callback-fields" [cacheWidgetCallback]="callback" [interactive]="interactive()" [busy]="busy()"
                        (run)="run.emit({ callback: callback.event, trigger: $event })"></dl>
                } @empty { <p class="panel-message">No cached callbacks.</p> }
            } @else { <p class="panel-message">Callback metadata is unavailable.</p> }
        </section>
    `,
    styleUrls: ["./widget-panels.scss"],
    styles: `
        :host { display: block; }
        .callback-fields + .callback-fields {
            border-top: 1px solid var(--workspace-border);
            margin-top: 0.75rem;
            padding-top: 0.75rem;
        }
    `,
})
export class WidgetLogicComponent {
    readonly component = input.required<WidgetComponent>();
    readonly callbacks = input.required<WidgetCallback[] | null>();
    readonly interactive = input.required<boolean>();
    readonly busy = input.required<boolean>();
    readonly run = output<{ callback: string; trigger: string }>();
    /** Context-menu options that are set, in slot order. */
    readonly options = computed(() => (this.component().options ?? []).filter(Boolean));
}
