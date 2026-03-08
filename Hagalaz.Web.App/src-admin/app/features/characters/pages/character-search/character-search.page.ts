import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { FormBuilder, ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatTableModule } from "@angular/material/table";
import { MatChipsModule } from "@angular/material/chips";
import { CharacterStore } from "../../services/character.store";
import { RunicCardComponent } from "../../../../core/components/runic-card/runic-card.component";

@Component({
    selector: "admin-character-search",
    standalone: true,
    imports: [
        ReactiveFormsModule,
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        MatIconModule,
        MatProgressSpinnerModule,
        MatTableModule,
        MatChipsModule,
        RunicCardComponent
    ],
    template: `
        <div class="space-y-6 animate-in">
            <!-- Standardized Header -->
            <header class="portal-page-header">
                <div class="title-area">
                    <div class="flex items-center gap-3 mb-2">
                        <span class="runic-label !text-storm-gold/60">Account Management</span>
                    </div>
                    <h1>Account Lookup</h1>
                    <p>Search and manage character accounts across the system.</p>
                </div>
            </header>

            <admin-runic-card title="Search Parameters">
                <form [formGroup]="searchForm" (ngSubmit)="search()" class="flex gap-4">
                    <mat-form-field appearance="outline" class="flex-1">
                        <mat-label>Username, Display Name or Email</mat-label>
                        <input matInput formControlName="query" placeholder="Search criteria..." />
                        <mat-icon matPrefix class="opacity-40">search</mat-icon>
                    </mat-form-field>
                    <button mat-flat-button color="primary" type="submit" [disabled]="store.loading()" class="h-[56px] !rounded-md !px-8">
                        Execute Search
                    </button>
                </form>
            </admin-runic-card>

            @if (store.results().length > 0 || store.hasSearched()) {
                @defer (on idle) {
                    <admin-runic-card title="Search Results">
                        <div class="overflow-hidden rounded-lg border border-white/5 bg-black/20">
                            <table mat-table [dataSource]="store.results()" class="!bg-transparent w-full">
                                <!-- ID Column -->
                                <ng-container matColumnDef="id">
                                    <th mat-header-cell *matHeaderCellDef class="!text-storm-gold/60 uppercase text-[10px] font-black tracking-widest">ID</th>
                                    <td mat-cell *matCellDef="let char" class="!text-storm-text-dim font-mono text-xs">#{{char.id}}</td>
                                </ng-container>

                                <!-- Name Column -->
                                <ng-container matColumnDef="name">
                                    <th mat-header-cell *matHeaderCellDef class="!text-storm-gold/60 uppercase text-[10px] font-black tracking-widest">Account Name</th>
                                    <td mat-cell *matCellDef="let char">
                                        <div class="flex flex-col py-2">
                                            <span class="text-storm-text font-bold">{{char.displayName}}</span>
                                            <span class="runic-label !text-storm-text-dim !lowercase !tracking-tighter">{{char.username}}</span>
                                        </div>
                                    </td>
                                </ng-container>

                                <!-- Status Column -->
                                <ng-container matColumnDef="status">
                                    <th mat-header-cell *matHeaderCellDef class="!text-storm-gold/60 uppercase text-[10px] font-black tracking-widest">Status</th>
                                    <td mat-cell *matCellDef="let char">
                                        <div class="flex gap-2">
                                            @if (char.isBanned) {
                                                <mat-chip class="!bg-rose-500/10 !text-rose-400 !border-rose-500/20 !text-[10px] h-6 font-bold uppercase tracking-widest">BANNED</mat-chip>
                                            } @else if (char.isMuted) {
                                                <mat-chip class="!bg-storm-gold/10 !text-storm-gold !border-storm-gold/20 !text-[10px] h-6 font-bold uppercase tracking-widest">MUTED</mat-chip>
                                            } @else {
                                                <mat-chip class="!bg-emerald-500/10 !text-emerald-400 !border-emerald-500/20 !text-[10px] h-6 font-bold uppercase tracking-widest">ACTIVE</mat-chip>
                                            }
                                        </div>
                                    </td>
                                </ng-container>

                                <!-- Actions Column -->
                                <ng-container matColumnDef="actions">
                                    <th mat-header-cell *matHeaderCellDef class="!text-storm-gold/60 uppercase text-[10px] font-black tracking-widest text-right">Actions</th>
                                    <td mat-cell *matCellDef="let char" class="text-right">
                                        <button mat-icon-button class="!text-storm-gold/60 hover:!text-storm-gold transition-colors" matTooltip="View Dossier">
                                            <mat-icon>visibility</mat-icon>
                                        </button>
                                        <button mat-icon-button class="!text-rose-500/60 hover:!text-rose-500 transition-colors" matTooltip="Apply Sanction">
                                            <mat-icon>gavel</mat-icon>
                                        </button>
                                    </td>
                                </ng-container>

                                <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
                                <tr mat-row *matRowDef="let row; columns: displayedColumns;" class="hover:bg-storm-gold/5 transition-colors cursor-pointer"></tr>
                            </table>

                            @if (store.results().length === 0 && store.hasSearched()) {
                                <div class="py-12 text-center">
                                    <mat-icon class="text-4xl text-storm-gold/20 mb-2 !w-auto !h-auto">person_off</mat-icon>
                                    <p class="m-0 text-storm-text-dim italic">No account found matching your query.</p>
                                </div>
                            }
                        </div>
                    </admin-runic-card>
                } @placeholder {
                    <div class="py-12 flex justify-center">
                        <mat-spinner diameter="40"></mat-spinner>
                    </div>
                }
            }
        </div>
    `,
    styles: [`
        :host { display: block; }
        ::ng-deep .mat-mdc-table {
            background: transparent !important;
        }
        ::ng-deep .mat-mdc-header-cell {
            border-bottom: 1px solid var(--color-storm-gold-dim) !important;
        }
        ::ng-deep .mat-mdc-cell {
            border-bottom: 1px solid rgba(251, 191, 36, 0.05) !important;
        }
    `],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CharacterSearchPageComponent {
    private readonly fb = inject(FormBuilder);
    readonly store = inject(CharacterStore);

    readonly searchForm = this.fb.nonNullable.group({
        query: [""]
    });

    readonly displayedColumns = ["id", "name", "status", "actions"];

    async search() {
        const { query } = this.searchForm.getRawValue();
        if (!query) return;
        await this.store.search(query);
    }
}
