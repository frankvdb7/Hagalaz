import { ChangeDetectionStrategy, Component, inject, OnInit } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { WorldStore } from "../../services/world.store";
import { RunicCardComponent } from "../../../../core/components/runic-card/runic-card.component";

@Component({
    selector: "admin-world-status",
    standalone: true,
    imports: [
        MatButtonModule,
        MatIconModule,
        MatProgressBarModule,
        MatProgressSpinnerModule,
        RunicCardComponent
    ],
    template: `
        <div class="space-y-8 animate-in">
            <!-- Standardized Header -->
            <header class="portal-page-header">
                <div class="title-area">
                    <div class="flex items-center gap-3 mb-2">
                        <span class="runic-label !text-storm-gold/60">Infrastructure Oversight</span>
                    </div>
                    <h1>Server Status</h1>
                    <p>Monitor the vital signs of the realm's infrastructure.</p>
                </div>
                <div class="action-area">
                    <button mat-stroked-button (click)="store.loadStatus()" [disabled]="store.loading()" class="!border-storm-gold/20 !text-storm-text !rounded-md">
                        <mat-icon class="mr-2">refresh</mat-icon>
                        Refresh Vitality
                    </button>
                </div>
            </header>

            <div class="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                @defer (on idle) {
                    @for (world of store.worlds(); track world.id) {
                        <admin-runic-card [title]="world.name">
                            <div class="space-y-6 py-2">
                                <div class="flex items-center justify-between">
                                    <div class="flex items-center gap-3">
                                        <div class="w-3 h-3 rounded-full animate-pulse" [class.bg-emerald-500]="world.isOnline" [class.bg-rose-500]="!world.isOnline"></div>
                                        <span class="runic-label !tracking-widest" [class.text-emerald-400]="world.isOnline" [class.text-rose-400]="!world.isOnline">
                                            {{ world.isOnline ? 'Online' : 'Offline' }}
                                        </span>
                                    </div>
                                    <span class="runic-label !text-storm-text-dim !font-mono">ID: {{world.id}}</span>
                                </div>

                                <div class="space-y-2">
                                    <div class="flex justify-between text-xs text-storm-text-muted">
                                        <span class="runic-label">Inhabitants</span>
                                        <span class="text-sm font-mono text-storm-text">{{world.playerCount}} / {{world.maxPlayers}}</span>
                                    </div>
                                    <mat-progress-bar mode="determinate" [value]="(world.playerCount / world.maxPlayers) * 100" class="h-2 rounded-full !bg-storm-bg-header/20"></mat-progress-bar>
                                </div>

                                <div class="grid grid-cols-2 gap-4">
                                    <div class="p-3 rounded bg-black/20 border border-white/5">
                                        <p class="runic-label !text-storm-gold/40 mb-1">Uptime</p>
                                        <p class="m-0 text-sm text-storm-text font-mono">{{world.uptime}}</p>
                                    </div>
                                    <div class="p-3 rounded bg-black/20 border border-white/5">
                                        <p class="runic-label !text-storm-gold/40 mb-1">Latency</p>
                                        <p class="m-0 text-sm text-emerald-400 font-mono">12ms</p>
                                    </div>
                                </div>

                                <div class="pt-2 flex gap-2">
                                    <button mat-stroked-button class="flex-1 !border-storm-gold/10 !text-[10px] uppercase font-bold tracking-widest hover:!bg-storm-gold/5">
                                        Broadcast
                                    </button>
                                    <button mat-stroked-button color="warn" class="!border-rose-500/10 !text-[10px] uppercase font-bold tracking-widest hover:!bg-rose-500/5">
                                        Sever
                                    </button>
                                </div>
                            </div>
                        </admin-runic-card>
                    }
                } @placeholder {
                    <div class="col-span-full py-12 flex justify-center">
                        <mat-spinner diameter="40"></mat-spinner>
                    </div>
                }
            </div>

            @defer (on idle) {
                <admin-runic-card title="System Events">
                    <div class="py-12 text-center opacity-40">
                        <mat-icon class="text-5xl mb-4 !w-auto !h-auto">event_note</mat-icon>
                        <p class="m-0 italic">No critical system events detected.</p>
                    </div>
                </admin-runic-card>
            } @placeholder {
                <admin-runic-card title="System Events">
                    <div class="py-12 flex justify-center"><mat-spinner diameter="40"></mat-spinner></div>
                </admin-runic-card>
            }
        </div>
    `,
    styles: [`
        :host { display: block; }
    `],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorldStatusPageComponent implements OnInit {
    readonly store = inject(WorldStore);

    ngOnInit() {
        this.store.loadStatus();
    }
}
