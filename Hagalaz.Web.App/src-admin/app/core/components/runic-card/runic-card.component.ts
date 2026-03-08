import { ChangeDetectionStrategy, Component, input } from "@angular/core";
import { MatCardModule } from "@angular/material/card";

@Component({
    selector: "admin-runic-card",
    standalone: true,
    imports: [MatCardModule],
    template: `
        <mat-card class="h-full border !border-storm-gold/30 !bg-storm-bg-panel !rounded-xl backdrop-blur-xl">
            @if (title()) {
                <mat-card-header class="!pt-3">
                    <div mat-card-title class="!font-serif !font-black !uppercase !tracking-[0.2em] !text-[11px] !text-storm-gold">{{ title() }}</div>
                </mat-card-header>
                <div class="mx-4 mb-2 h-px bg-linear-to-r from-storm-gold/40 to-transparent"></div>
            }
            <mat-card-content>
                <ng-content></ng-content>
            </mat-card-content>
        </mat-card>
    `,
    styles: [`
        :host { display: block; height: 100%; }
    `],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RunicCardComponent {
    title = input<string>();
}
