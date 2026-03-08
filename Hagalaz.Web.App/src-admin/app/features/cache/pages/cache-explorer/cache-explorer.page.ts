import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatTooltipModule } from "@angular/material/tooltip";
import { MatDividerModule } from "@angular/material/divider";
import { MatListModule } from "@angular/material/list";
import { MatMenuModule } from "@angular/material/menu";
import { CacheExplorerStore } from "../../services/cache-explorer.store";
import { RunicCardComponent } from "../../../../core/components/runic-card/runic-card.component";

@Component({
    selector: "admin-cache-explorer",
    standalone: true,
    imports: [
        CommonModule,
        MatButtonModule,
        MatIconModule,
        MatProgressBarModule,
        MatProgressSpinnerModule,
        MatTooltipModule,
        MatDividerModule,
        MatListModule,
        MatMenuModule,
        RunicCardComponent
    ],
    template: `
        <div class="space-y-6 animate-in">
            <!-- Standardized Header -->
            <header class="portal-page-header">
                <div class="title-area">
                    <div class="flex items-center gap-3 mb-2">
                        <span class="runic-label !text-storm-gold/60">Core Registry</span>
                        @if (store.error()) {
                            <span class="text-rose-400 text-xs font-medium animate-pulse">{{ store.error() }}</span>
                        }
                    </div>
                    <h1>Explorer</h1>
                    <p>Deep traversal of the core game data hierarchy.</p>
                </div>
                <div class="action-area">
                    <button mat-stroked-button (click)="store.loadIndices()" [disabled]="store.loading()" class="!border-storm-gold/20 !text-storm-text !rounded-md">
                        <mat-icon class="mr-2">sync</mat-icon>
                        Refresh Root
                    </button>
                </div>
            </header>

            <div class="grid gap-4 lg:grid-cols-3 h-[calc(100vh-280px)]">
                <!-- Column 1: Indices -->
                @defer (on idle) {
                    <admin-runic-card title="Indices" class="h-full">
                        <mat-selection-list [multiple]="false" class="h-full overflow-y-auto pr-2" (selectionChange)="onIndexSelect($event.options[0].value)">
                            @for (idx of store.indices(); track idx.indexId) {
                                <mat-list-option [value]="idx.indexId" [selected]="selectedIdx() === idx.indexId" class="explorer-option">
                                    <mat-icon matListItemIcon class="!text-storm-gold/40">folder_zip</mat-icon>
                                    <div matListItemTitle class="!text-storm-text/90 !text-sm">Index {{ idx.indexId }}</div>
                                    <div matListItemLine class="runic-label !text-storm-text-dim !font-mono">v{{ idx.version }} • CRC: {{ idx.crc32 }}</div>
                                </mat-list-option>
                            }
                        </mat-selection-list>
                    </admin-runic-card>
                } @placeholder {
                    <admin-runic-card title="Indices" class="h-full">
                        <div class="p-12 flex justify-center"><mat-spinner diameter="40"></mat-spinner></div>
                    </admin-runic-card>
                }

                <!-- Column 2: Archives -->
                @defer (on idle) {
                    <admin-runic-card title="Archives" class="h-full">
                        @if (selectedIdx() !== null) {
                            <mat-selection-list [multiple]="false" class="h-full overflow-y-auto pr-2" (selectionChange)="onArchiveSelect($event.options[0].value)">
                                @if (store.selectedArchiveEntries().length === 0 && !store.loading()) {
                                    <div class="py-12 text-center opacity-20 italic text-sm">No archives found.</div>
                                }
                                @for (arc of store.selectedArchiveEntries(); track arc.fileId) {
                                    <mat-list-option [value]="arc.fileId" [selected]="selectedArc() === arc.fileId" class="explorer-option">
                                        <mat-icon matListItemIcon class="!text-storm-gold/40">inventory_2</mat-icon>
                                        <div matListItemTitle class="!text-storm-text/90 !text-sm">Archive {{ arc.fileId }}</div>
                                        <div matListItemLine class="runic-label !text-storm-text-dim !font-mono">{{ arc.capacity }} members • v{{ arc.version }}</div>
                                    </mat-list-option>
                                }
                            </mat-selection-list>
                        } @else {
                            <div class="h-full flex flex-col items-center justify-center opacity-20 text-center px-8">
                                <mat-icon class="text-5xl mb-2">folder_open</mat-icon>
                                <p class="text-xs">Select an index to browse archives</p>
                            </div>
                        }
                    </admin-runic-card>
                } @placeholder {
                    <admin-runic-card title="Archives" class="h-full">
                        <div class="p-12 flex justify-center"><mat-spinner diameter="40"></mat-spinner></div>
                    </admin-runic-card>
                }

                <!-- Column 3: Member Files -->
                @defer (on idle) {
                    <admin-runic-card title="Member Files" class="h-full">
                        @if (selectedArc() !== null) {
                            <mat-list class="h-full overflow-y-auto pr-2">
                                @if (store.selectedMemberFiles().length === 0 && !store.loading()) {
                                    <div class="py-12 text-center opacity-20 italic text-sm">No members found.</div>
                                }
                                @for (sub of store.selectedMemberFiles(); track sub.subFileId) {
                                    <mat-list-item class="explorer-option !cursor-default">
                                        <mat-icon matListItemIcon class="!text-storm-gold/40">description</mat-icon>
                                        <div matListItemTitle class="!text-storm-text/90 !text-sm">File {{ sub.subFileId }}</div>
                                        <div matListItemLine class="runic-label !text-storm-text-dim !font-mono">{{ (sub.dataLength / 1024) | number:'1.1-2' }} KB</div>
                                        <button mat-icon-button matListItemMeta class="!text-storm-gold/40 hover:!text-storm-gold transition-colors"
                                                matTooltip="Download raw data">
                                            <mat-icon>download</mat-icon>
                                        </button>
                                    </mat-list-item>
                                }
                            </mat-list>
                        } @else {
                            <div class="h-full flex flex-col items-center justify-center opacity-20 text-center px-8">
                                <mat-icon class="text-5xl mb-2">find_in_page</mat-icon>
                                <p class="text-xs">Select an archive to inspect members</p>
                            </div>
                        }
                    </admin-runic-card>
                } @placeholder {
                    <admin-runic-card title="Member Files" class="h-full">
                        <div class="p-12 flex justify-center"><mat-spinner diameter="40"></mat-spinner></div>
                    </admin-runic-card>
                }
            </div>

            @if (store.loading()) {
                <mat-progress-bar mode="indeterminate" class="!fixed bottom-0 left-0 right-0 z-[100]"></mat-progress-bar>
            }
        </div>
    `,
    styles: [`
        :host { display: block; }
        
        .explorer-option {
            --mdc-list-list-item-container-color: rgba(255, 255, 255, 0.02);
            --mdc-list-list-item-selected-container-color: var(--color-storm-gold-dim);
            margin-bottom: 2px;
            border-radius: 4px;
            border: 1px solid transparent;
            transition: all 0.2s ease;

            &:hover {
                --mdc-list-list-item-container-color: rgba(251, 191, 36, 0.05);
                border-color: var(--color-storm-gold-dim);
            }

            &.mdc-list-item--selected {
                border-color: rgba(251, 191, 36, 0.3);
                border-left: 3px solid var(--color-storm-gold);
                border-top-left-radius: 0;
                border-bottom-left-radius: 0;
            }
        }
    `],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CacheExplorerPageComponent implements OnInit {
    readonly store = inject(CacheExplorerStore);

    readonly selectedIdx = signal<number | null>(null);
    readonly selectedArc = signal<number | null>(null);

    ngOnInit() {
        this.store.loadIndices();
    }

    onIndexSelect(id: number) {
        if (this.selectedIdx() === id) return;
        this.selectedIdx.set(id);
        this.selectedArc.set(null);
        this.store.loadArchives(id);
    }

    onArchiveSelect(id: number) {
        if (this.selectedArc() === id) return;
        this.selectedArc.set(id);
        const indexId = this.selectedIdx();
        if (indexId !== null) {
            this.store.loadMemberFiles({ indexId, fileId: id });
        }
    }
}
