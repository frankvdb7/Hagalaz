import { ChangeDetectionStrategy, Component, inject, OnInit, computed } from "@angular/core";
import { CommonModule } from "@angular/common";
import { MatButtonModule } from "@angular/material/button";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatChipsModule } from "@angular/material/chips";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltipModule } from "@angular/material/tooltip";
import { signalStore, withState, withMethods, patchState, withComputed } from "@ngrx/signals";
import { firstValueFrom } from "rxjs";
import { CacheTypesStore } from "../../services/cache-types.store";
import { WorldStore } from "../../../world/services/world.store";
import { RunicCardComponent } from "../../../../core/components/runic-card/runic-card.component";
import { AuthStore } from "../../../../core/auth/auth.store";
import { SkeletonComponent } from "../../../../core/components/skeleton/skeleton.component";

interface DashboardInternalState {
    systemHealth: {
        cpu: number;
        memory: number;
        storage: number;
        network: number;
    };
    loading: boolean;
}

const initialState: DashboardInternalState = {
    systemHealth: {
        cpu: 12,
        memory: 45,
        storage: 68,
        network: 5,
    },
    loading: false,
};

const DashboardStore = signalStore(
    withState(initialState),
    withComputed((state) => ({
        overallStatus: computed(() => {
            const h = state.systemHealth();
            if (h.cpu > 90 || h.memory > 90) return 'CRITICAL';
            if (h.cpu > 70 || h.memory > 80) return 'WARNING';
            return 'HEALTHY';
        })
    })),
    withMethods((store) => ({
        refreshHealth(): void {
            patchState(store, {
                systemHealth: {
                    cpu: Math.floor(Math.random() * 30) + 5,
                    memory: Math.floor(Math.random() * 20) + 40,
                    storage: 68,
                    network: Math.floor(Math.random() * 15),
                }
            });
        }
    }))
);

@Component({
    selector: "admin-cache-dashboard",
    standalone: true,
    providers: [DashboardStore],
    imports: [
        CommonModule,
        MatButtonModule, 
        MatProgressBarModule, 
        MatChipsModule, 
        MatIconModule, 
        MatTooltipModule,
        RunicCardComponent,
        SkeletonComponent
    ],
    template: `
        <div class="mx-auto max-w-[1600px] space-y-8 animate-in">
            
            <header class="portal-page-header">
                <div class="title-area">
                    <div class="mb-2 flex items-center gap-3">
                        <mat-chip class="!h-6 !border-emerald-500/20 !bg-emerald-500/10 !text-[10px] !font-bold !tracking-widest uppercase !text-emerald-400">System {{ dashboardStore.overallStatus() }}</mat-chip>
                        <span class="runic-label !text-storm-text-dim">Active Session: 4h 12m</span>
                    </div>
                    <h1>Command Center</h1>
                    <p>Administrative access granted. Oversee infrastructure and manage game registries.</p>
                </div>
                <div class="action-area flex items-center gap-4">
                    <button mat-flat-button color="primary" class="!h-11 !rounded-md !px-6">
                        <mat-icon class="mr-2">security</mat-icon>
                        System Audit
                    </button>
                    <button mat-stroked-button class="!h-11 !rounded-md !border-storm-gold/20 !px-6 !text-storm-text-muted hover:!bg-storm-gold/5">
                        <mat-icon class="mr-2">analytics</mat-icon>
                        Traffic Logs
                    </button>
                </div>
            </header>

            <div class="grid gap-6 lg:grid-cols-[1fr_350px]">
                <div class="glass relative overflow-hidden p-8 flex flex-col justify-center">
                    <div class="absolute right-0 top-0 h-96 w-96 translate-x-1/3 -translate-y-1/3 rounded-full bg-storm-gold/5 blur-[120px]"></div>
                    <div class="relative z-10">
                        <h2 class="m-0 font-serif text-2xl tracking-tight text-storm-text">Registry Oversight</h2>
                        <p class="m-0 mt-2 max-w-2xl text-lg font-serif italic leading-relaxed text-storm-text-muted">
                            The infrastructure is operating within normal parameters. 
                            All synchronization gates are currently sealed and secure.
                        </p>
                    </div>
                </div>

                <admin-runic-card title="Archivist Profile">
                    <div class="flex flex-col items-center py-4 text-center">
                        <div class="relative mb-4">
                            <div class="h-20 w-20 rounded-full bg-linear-to-tr from-storm-gold to-storm-gold/40 p-1 shadow-lg shadow-black/40">
                                <div class="flex h-full w-full items-center justify-center rounded-full bg-storm-bg-header">
                                    <mat-icon class="!text-4xl text-storm-gold">shield_person</mat-icon>
                                </div>
                            </div>
                            <div class="absolute bottom-0 right-0 h-6 w-6 rounded-full border-4 border-storm-bg-header bg-emerald-500 shadow-sm" matTooltip="Online"></div>
                        </div>
                        <h3 class="m-0 font-serif text-xl tracking-wide text-storm-text">{{ authStore.user()?.preferred_username }}</h3>
                        <p class="m-0 mb-6 text-[10px] font-black uppercase tracking-[0.2em] text-storm-gold">Level 99 High Archivist</p>
                        
                        <div class="grid w-full grid-cols-2 gap-2">
                            <div class="rounded border border-white/5 bg-black/30 p-3">
                                <p class="m-0 mb-1 text-[10px] font-bold uppercase text-storm-text-dim">Actions</p>
                                <p class="m-0 font-mono text-sm text-storm-text">1,248</p>
                            </div>
                            <div class="rounded border border-white/5 bg-black/30 p-3">
                                <p class="m-0 mb-1 text-[10px] font-bold uppercase text-storm-text-dim">Reputation</p>
                                <p class="m-0 font-mono text-sm text-storm-text">EXALTED</p>
                            </div>
                        </div>
                    </div>
                </admin-runic-card>
            </div>

            <div class="grid gap-6 lg:grid-cols-3">
                
                <admin-runic-card title="Infrastructure Radar" class="lg:col-span-2">
                    <div class="grid gap-8 py-4 md:grid-cols-2 lg:grid-cols-4">
                        <div class="space-y-3">
                            <div class="flex items-end justify-between">
                                <span class="runic-label">CPU Load</span>
                                <span class="font-mono text-sm text-storm-text">{{ dashboardStore.systemHealth().cpu }}%</span>
                            </div>
                            <div class="h-1.5 w-full overflow-hidden rounded-full bg-black/40">
                                <div class="h-full bg-linear-to-r from-storm-gold to-storm-gold/40 transition-all duration-1000" [style.width.%]="dashboardStore.systemHealth().cpu"></div>
                            </div>
                            <p class="text-[9px] italic text-storm-text-dim">8 Core / 16 Thread Processor</p>
                        </div>

                        <div class="space-y-3">
                            <div class="flex items-end justify-between">
                                <span class="runic-label">Memory</span>
                                <span class="font-mono text-sm text-storm-text">{{ dashboardStore.systemHealth().memory }}%</span>
                            </div>
                            <div class="h-1.5 w-full overflow-hidden rounded-full bg-black/40">
                                <div class="h-full bg-linear-to-r from-blue-600 to-blue-400 transition-all duration-1000" [style.width.%]="dashboardStore.systemHealth().memory"></div>
                            </div>
                            <p class="text-[9px] italic text-storm-text-dim">Allocated: 14.2GB / 32GB</p>
                        </div>

                        <div class="space-y-3">
                            <div class="flex items-end justify-between">
                                <span class="runic-label">Registry</span>
                                <span class="font-mono text-sm text-storm-text">{{ dashboardStore.systemHealth().storage }}%</span>
                            </div>
                            <div class="h-1.5 w-full overflow-hidden rounded-full bg-black/40">
                                <div class="h-full bg-linear-to-r from-emerald-600 to-emerald-400 transition-all duration-1000" [style.width.%]="dashboardStore.systemHealth().storage"></div>
                            </div>
                            <p class="text-[9px] italic text-storm-text-dim">Entity Definition Volume</p>
                        </div>

                        <div class="space-y-3">
                            <div class="flex items-end justify-between">
                                <span class="runic-label">Network</span>
                                <span class="font-mono text-sm text-storm-text">{{ dashboardStore.systemHealth().network }}%</span>
                            </div>
                            <div class="h-1.5 w-full overflow-hidden rounded-full bg-black/40">
                                <div class="h-full bg-linear-to-r from-purple-600 to-purple-400 transition-all duration-1000" [style.width.%]="dashboardStore.systemHealth().network"></div>
                            </div>
                            <p class="text-[9px] italic text-storm-text-dim">Ingress: 4.2 MB/s</p>
                        </div>
                    </div>
                    
                    <div class="mt-8 flex items-center justify-between border-t border-storm-gold/10 pt-6">
                        <div class="flex gap-6">
                            <div class="flex items-center gap-2">
                                <div class="h-2 w-2 rounded-full bg-emerald-500"></div>
                                <span class="text-[10px] font-bold uppercase tracking-tighter text-storm-text-muted">Auth Service: ACTIVE</span>
                            </div>
                            <div class="flex items-center gap-2">
                                <div class="h-2 w-2 rounded-full bg-emerald-500"></div>
                                <span class="text-[10px] font-bold uppercase tracking-tighter text-storm-text-muted">Cache API: ACTIVE</span>
                            </div>
                            <div class="flex items-center gap-2">
                                <div class="h-2 w-2 animate-pulse rounded-full bg-storm-gold"></div>
                                <span class="text-[10px] font-bold uppercase tracking-tighter text-storm-text-muted">World Hub: SYNCING</span>
                            </div>
                        </div>
                        <button mat-icon-button (click)="dashboardStore.refreshHealth()" class="!text-storm-gold/40 transition-colors hover:!text-storm-gold">
                            <mat-icon>sync</mat-icon>
                        </button>
                    </div>
                </admin-runic-card>

                <admin-runic-card title="Population Metrics">
                    <div class="flex h-full flex-col justify-between py-2">
                        <div class="space-y-6">
                            <div class="flex items-center justify-between">
                                <div class="flex items-center gap-3">
                                    <div class="flex h-10 w-10 items-center justify-center rounded-lg border border-emerald-500/20 bg-emerald-500/5">
                                        <mat-icon class="text-emerald-500">groups</mat-icon>
                                    </div>
                                    <div>
                                        <p class="m-0 text-xs font-bold uppercase tracking-tighter text-storm-text">Global Active</p>
                                        <p class="m-0 font-serif text-2xl text-emerald-400">{{ totalPlayers() }}</p>
                                    </div>
                                </div>
                                <div class="text-right">
                                    <p class="m-0 text-[10px] font-black uppercase text-storm-text-dim">Peak Today</p>
                                    <p class="m-0 font-mono text-sm text-storm-text-muted">482</p>
                                </div>
                            </div>

                            @defer (on timer(500ms)) {
                                <div class="space-y-2">
                                    @for (world of worldStore.worlds(); track world.id) {
                                        <div class="flex items-center gap-3">
                                            <span class="w-4 font-mono text-[10px] text-storm-gold/40">#{{world.id}}</span>
                                            <span class="flex-1 truncate text-xs text-storm-text">{{world.name}}</span>
                                            <span class="font-mono text-xs text-storm-text-muted">{{world.playerCount}}</span>
                                            <div class="h-1 w-16 overflow-hidden rounded-full bg-black/40">
                                                <div class="h-full bg-emerald-500/40" [style.width.%]="(world.playerCount/world.maxPlayers)*100"></div>
                                            </div>
                                        </div>
                                    }
                                </div>
                            } @placeholder {
                                <div class="space-y-4">
                                    <admin-skeleton height="12px"></admin-skeleton>
                                    <admin-skeleton height="12px"></admin-skeleton>
                                    <admin-skeleton height="12px"></admin-skeleton>
                                </div>
                            }
                        </div>

                        <button mat-stroked-button routerLink="/portal/world/status" class="mt-4 w-full !border-storm-gold/10 !text-[10px] !font-bold !tracking-widest uppercase !text-storm-gold/60">
                            Expand Status
                        </button>
                    </div>
                </admin-runic-card>
            </div>

            <div class="grid gap-6 lg:grid-cols-[1fr_1.5fr]">
                
                <div class="grid grid-cols-2 gap-4">
                    <admin-runic-card title="Definitions">
                        <div class="py-4 text-center">
                            <mat-icon class="!text-4xl text-storm-gold/20 mb-2">inventory_2</mat-icon>
                            @defer (when !cacheStore.loading()) {
                                <p class="m-0 font-serif text-3xl text-storm-text">{{ cacheStore.archiveSizes()?.items ?? '...' }}</p>
                            } @placeholder {
                                <div class="flex justify-center py-2"><admin-skeleton width="60px" height="24px"></admin-skeleton></div>
                            }
                            <p class="m-0 text-[9px] font-black uppercase tracking-widest text-storm-gold/60">Total Items</p>
                        </div>
                    </admin-runic-card>
                    <admin-runic-card title="NPC Database">
                        <div class="py-4 text-center">
                            <mat-icon class="!text-4xl text-storm-gold/20 mb-2">pets</mat-icon>
                            @defer (when !cacheStore.loading()) {
                                <p class="m-0 font-serif text-3xl text-storm-text">{{ cacheStore.archiveSizes()?.npcs ?? '...' }}</p>
                            } @placeholder {
                                <div class="flex justify-center py-2"><admin-skeleton width="60px" height="24px"></admin-skeleton></div>
                            }
                            <p class="m-0 text-[9px] font-black uppercase tracking-widest text-storm-gold/60">Unique NPCs</p>
                        </div>
                    </admin-runic-card>
                    <admin-runic-card title="Asset Store">
                        <div class="py-4 text-center">
                            <mat-icon class="!text-4xl text-storm-gold/20 mb-2">collections</mat-icon>
                            @defer (when !cacheStore.loading()) {
                                <p class="m-0 font-serif text-3xl text-storm-text">{{ cacheStore.archiveSizes()?.sprites ?? '...' }}</p>
                            } @placeholder {
                                <div class="flex justify-center py-2"><admin-skeleton width="60px" height="24px"></admin-skeleton></div>
                            }
                            <p class="m-0 text-[9px] font-black uppercase tracking-widest text-storm-gold/60">Sprite Assets</p>
                        </div>
                    </admin-runic-card>
                    <admin-runic-card title="System Uptime">
                        <div class="py-4 text-center">
                            <mat-icon class="!text-4xl text-storm-gold/20 mb-2">dns</mat-icon>
                            <p class="m-0 font-serif text-3xl text-storm-text">99.9%</p>
                            <p class="m-0 text-[9px] font-black uppercase tracking-widest text-storm-gold/60">Core Stability</p>
                        </div>
                    </admin-runic-card>
                </div>

                <admin-runic-card title="Audit Ledger (Recent)">
                    <div class="space-y-1 py-2">
                        @defer (on timer(800ms)) {
                            @for (activity of recentActivities; track activity.id) {
                                <div class="group flex items-center gap-4 border-b border-white/5 p-3 transition-all last:border-0 hover:bg-storm-gold/5">
                                    <div class="flex h-8 w-8 items-center justify-center rounded border border-storm-gold/10 bg-storm-gold/5 text-storm-gold/40 transition-all group-hover:border-storm-gold/30 group-hover:text-storm-gold">
                                        <mat-icon class="!text-lg">{{ activity.icon }}</mat-icon>
                                    </div>
                                    <div class="flex min-w-0 flex-1 items-center justify-between">
                                        <div class="min-w-0">
                                            <p class="m-0 truncate text-xs font-bold text-storm-text">{{ activity.action }}</p>
                                            <p class="m-0 truncate text-[10px] text-storm-text-dim">{{ activity.details }}</p>
                                        </div>
                                        <div class="ml-4 flex-shrink-0 text-right">
                                            <span class="text-[9px] font-black uppercase tracking-tighter text-storm-gold/40">{{ activity.time }}</span>
                                        </div>
                                    </div>
                                </div>
                            }
                        } @placeholder {
                            <div class="space-y-4 p-4">
                                <admin-skeleton height="40px"></admin-skeleton>
                                <admin-skeleton height="40px"></admin-skeleton>
                                <admin-skeleton height="40px"></admin-skeleton>
                                <admin-skeleton height="40px"></admin-skeleton>
                            </div>
                        }
                    </div>
                    <div class="mt-4 text-center">
                        <button mat-button color="primary" class="!text-[10px] font-black uppercase tracking-widest">View Eternal Ledger</button>
                    </div>
                </admin-runic-card>
            </div>
        </div>
    `,
    styles: [`
        :host { display: block; }
    `],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPageComponent implements OnInit {
    readonly dashboardStore = inject(DashboardStore);
    readonly cacheStore = inject(CacheTypesStore);
    readonly worldStore = inject(WorldStore);
    readonly authStore = inject(AuthStore);

    readonly totalPlayers = computed(() => 
        this.worldStore.worlds().reduce((acc, w) => acc + w.playerCount, 0)
    );

    readonly recentActivities = [
        { id: 1, action: "Entity Modification", details: "NPC 159 (Iron Dragon) Combat Level set to 185", time: "2h ago", icon: "edit_attributes" },
        { id: 2, action: "Asset Ingestion", details: "New PNG frame registered for Asset 2501", time: "5h ago", icon: "upload_file" },
        { id: 3, action: "Registry Access", details: "Range query executed on 'Items' partition", time: "Yesterday", icon: "manage_search" },
        { id: 4, action: "System Config", details: "Object definition update for 'Ancient Altar'", time: "2d ago", icon: "settings" },
        { id: 5, action: "Moderation", details: "Account 'GoldFarmer99' status set to PERMANENT_BAN", time: "3d ago", icon: "gavel" },
    ];

    ngOnInit() {
        this.cacheStore.loadArchiveSizes();
        this.worldStore.loadStatus();
        this.dashboardStore.refreshHealth();
    }
}
