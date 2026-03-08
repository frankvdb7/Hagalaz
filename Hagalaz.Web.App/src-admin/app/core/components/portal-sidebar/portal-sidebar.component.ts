import { ChangeDetectionStrategy, Component, inject, output } from "@angular/core";
import { RouterLink, RouterLinkActive } from "@angular/router";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatListModule } from "@angular/material/list";
import { AuthStore } from "../../auth/auth.store";

@Component({
    selector: "admin-portal-sidebar",
    standalone: true,
    imports: [RouterLink, RouterLinkActive, MatButtonModule, MatIconModule, MatListModule],
    template: `
        <div class="flex h-full flex-col border-r border-storm-gold/10 bg-storm-bg-panel/80 backdrop-blur-xl">
            <!-- Sidebar Header / Brand -->
            <div class="mb-2 border-b border-storm-gold/10 p-6">
                <div class="flex items-center gap-3">
                    <div class="flex h-10 w-10 items-center justify-center rounded border border-storm-gold/20 bg-linear-to-br from-storm-gold to-storm-bg-header shadow-lg shadow-black/40">
                        <mat-icon class="!font-bold !text-storm-text">auto_fix_high</mat-icon>
                    </div>
                    <div>
                        <h2 class="m-0 font-serif text-lg tracking-wider text-storm-text">Hagalaz</h2>
                        <p class="runic-label">Portal Admin</p>
                    </div>
                </div>
            </div>

            <!-- Navigation Links -->
            <mat-nav-list class="flex-1 px-2 py-2">
                <div mat-subheader class="runic-label !h-auto !py-4 opacity-60">Core</div>
                <a mat-list-item routerLink="/portal/dashboard" routerLinkActive="active-link" class="nav-item group">
                    <mat-icon matListItemIcon class="group-hover:!text-storm-gold/70 transition-colors">dashboard</mat-icon>
                    <span matListItemTitle>Dashboard</span>
                </a>

                <div mat-subheader class="runic-label !h-auto !py-4 opacity-60">Cache & Assets</div>
                <a mat-list-item routerLink="/portal/cache/types" routerLinkActive="active-link" class="nav-item group">
                    <mat-icon matListItemIcon class="group-hover:!text-storm-gold/70 transition-colors">inventory_2</mat-icon>
                    <span matListItemTitle>Definitions</span>
                </a>
                <a mat-list-item routerLink="/portal/cache/sprites" routerLinkActive="active-link" class="nav-item group">
                    <mat-icon matListItemIcon class="group-hover:!text-storm-gold/70 transition-colors">image</mat-icon>
                    <span matListItemTitle>Sprites</span>
                </a>
                <a mat-list-item routerLink="/portal/cache/explorer" routerLinkActive="active-link" class="nav-item group">
                    <mat-icon matListItemIcon class="group-hover:!text-storm-gold/70 transition-colors">account_tree</mat-icon>
                    <span matListItemTitle>Explorer</span>
                </a>
                <a mat-list-item routerLink="/portal/cache/maps" routerLinkActive="active-link" class="nav-item group">
                    <mat-icon matListItemIcon class="group-hover:!text-storm-gold/70 transition-colors">map</mat-icon>
                    <span matListItemTitle>Maps</span>
                </a>

                <div mat-subheader class="runic-label !h-auto !py-4 opacity-60">Characters</div>
                <a mat-list-item routerLink="/portal/characters/search" routerLinkActive="active-link" class="nav-item group">
                    <mat-icon matListItemIcon class="group-hover:!text-storm-gold/70 transition-colors">person_search</mat-icon>
                    <span matListItemTitle>Account Lookup</span>
                </a>
                <a mat-list-item routerLink="/portal/characters/offences" routerLinkActive="active-link" class="nav-item group">
                    <mat-icon matListItemIcon class="group-hover:!text-storm-gold/70 transition-colors">gavel</mat-icon>
                    <span matListItemTitle>Moderation</span>
                </a>

                <div mat-subheader class="runic-label !h-auto !py-4 opacity-60">Infrastructure</div>
                <a mat-list-item routerLink="/portal/world/status" routerLinkActive="active-link" class="nav-item group">
                    <mat-icon matListItemIcon class="group-hover:!text-storm-gold/70 transition-colors">dns</mat-icon>
                    <span matListItemTitle>Server Status</span>
                </a>
                <a mat-list-item routerLink="/portal/world/spawns" routerLinkActive="active-link" class="nav-item group">
                    <mat-icon matListItemIcon class="group-hover:!text-storm-gold/70 transition-colors">location_on</mat-icon>
                    <span matListItemTitle>Entity Spawns</span>
                </a>

                <div mat-subheader class="runic-label !h-auto !py-4 opacity-60">Logs</div>
                <a mat-list-item routerLink="/portal/logs/chat" routerLinkActive="active-link" class="nav-item group">
                    <mat-icon matListItemIcon class="group-hover:!text-storm-gold/70 transition-colors">chat</mat-icon>
                    <span matListItemTitle>Chat Logs</span>
                </a>
                <a mat-list-item routerLink="/portal/logs/audit" routerLinkActive="active-link" class="nav-item group">
                    <mat-icon matListItemIcon class="group-hover:!text-storm-gold/70 transition-colors">history_edu</mat-icon>
                    <span matListItemTitle>Audit Logs</span>
                </a>
            </mat-nav-list>

            <!-- User Section / Bottom -->
            <div class="mt-auto border-t border-storm-gold/10 bg-black/20 p-4">
                <button mat-stroked-button color="warn" (click)="logout.emit()" class="w-full !rounded-md !border-rose-500/20 !text-rose-400 hover:!bg-rose-500/5">
                    <mat-icon class="mr-2 !text-sm">logout</mat-icon>
                    <span class="text-xs font-bold uppercase tracking-widest">Leave Portal</span>
                </button>
            </div>
        </div>
    `,
    styles: [`
        :host { display: block; height: 100%; }
        
        .active-link {
            border-left: 3px solid var(--color-storm-gold) !important;
            background-color: var(--color-storm-gold-dim) !important;
            --mdc-list-item-label-text-color: var(--color-storm-gold);
            mat-icon { color: var(--color-storm-gold) !important; }
        }

        .nav-item {
            margin-bottom: 0.125rem;
            transition: all 0.2s ease;
            --mdc-list-item-label-text-size: 13px;
            --mdc-list-item-label-text-color: var(--color-storm-text-muted);
            
            mat-icon { color: var(--color-storm-gold-muted); }

            &:hover {
                background-color: var(--color-storm-gold-dim) !important;
                --mdc-list-item-label-text-color: var(--color-storm-text);
            }
        }
    `],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PortalSidebarComponent {
    readonly store = inject(AuthStore);
    logout = output<void>();
}
