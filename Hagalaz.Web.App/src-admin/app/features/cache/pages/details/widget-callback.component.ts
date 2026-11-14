import { Component, computed, input, output } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { RouterLink } from "@angular/router";
import type { WidgetCallback } from "../../services/cache-widgets.service";

/** One cached CS2 callback of a widget component, with a manual run form while the preview is interactive. */
@Component({
    selector: "dl[cacheWidgetCallback]",
    imports: [MatButtonModule, RouterLink],
    template: `
        <dt>Event</dt><dd>{{ callback().event }}</dd>
        <dt>Script</dt>
        <dd>
            @if (callback().scriptId !== null) {
                <a [routerLink]="['/cache/types/cs2', callback().scriptId]">Script {{ callback().scriptId }}</a>
            } @else { Unknown target }
        </dd>
        @if (callback().args.length) {
            <dt>Arguments</dt>
            <dd><ol>@for (argument of formattedArguments(); track $index) { <li><code>{{ argument }}</code></li> }</ol></dd>
        }
        @if (callback().triggers.length) {
            <dt>Trigger IDs</dt><dd>{{ callback().triggers.join(', ') }}</dd>
        }
        @if (runnable()) {
            <dt>Preview</dt>
            <dd>
                <form (submit)="$event.preventDefault(); run.emit(trigger.value)">
                    <input #trigger type="number" aria-label="Trigger ID" [hidden]="!callback().triggers.length" [required]="callback().triggers.length > 0" />
                    <button mat-button type="submit" [disabled]="busy()">Run callback</button>
                </form>
            </dd>
        }
    `,
    styles: `
        :host dd { margin: 0; overflow-wrap: anywhere; }
        ol { margin: 0; }
    `,
})
export class WidgetCallbackComponent {
    readonly callback = input.required<WidgetCallback>({ alias: "cacheWidgetCallback" });
    readonly interactive = input.required<boolean>();
    readonly busy = input.required<boolean>();
    /** Emits the entered trigger ID (empty when the callback has no triggers). */
    readonly run = output<string>();
    readonly runnable = computed(() => this.interactive() && this.callback().scriptId !== null);
    readonly formattedArguments = computed(() => this.callback().args.map(argument =>
        `${argument.type} ${argument.type === "string" ? `"${argument.value}"` : argument.value}`));
}
