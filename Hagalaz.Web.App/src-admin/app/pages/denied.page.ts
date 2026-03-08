import { ChangeDetectionStrategy, Component } from "@angular/core";
import { RouterLink } from "@angular/router";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { RunicCardComponent } from "../core/components/runic-card/runic-card.component";

@Component({
    standalone: true,
    selector: "admin-denied-page",
    imports: [RouterLink, MatButtonModule, MatIconModule, RunicCardComponent],
    template: `
        <section class="min-h-screen grid place-items-center p-4">
            <div class="w-full max-w-lg space-y-6">
                <header class="portal-page-header !mb-0 !bg-rose-950/20 !border-rose-500/20">
                    <div class="title-area !text-center w-full">
                        <div class="flex justify-center mb-2">
                            <span class="runic-label !text-rose-400">Security Protocol</span>
                        </div>
                        <h1 class="!text-rose-500">Forbidden Area</h1>
                    </div>
                </header>

                <admin-runic-card title="Unauthorized Access">
                    <div class="py-8 text-center space-y-6">
                        <mat-icon class="!text-6xl !text-rose-500/80 !w-auto !h-auto block mx-auto">lock_person</mat-icon>
                        
                        <p class="text-storm-text-muted font-serif italic text-lg leading-relaxed">
                            "Your account does not have the required permissions to access this administrative sector."
                        </p>

                        <div class="pt-4 flex justify-center">
                            <a mat-flat-button routerLink="/login" class="!px-8 !h-12 !rounded-md !font-serif !tracking-widest">
                                Return To Login
                            </a>
                        </div>
                    </div>
                </admin-runic-card>
            </div>
        </section>
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessDeniedPageComponent {}
