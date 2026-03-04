import { ChangeDetectionStrategy, Component } from "@angular/core";
import { RouterLink } from "@angular/router";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";

@Component({
    standalone: true,
    selector: "admin-denied-page",
    imports: [RouterLink, MatButtonModule, MatCardModule],
    template: `
        <section class="min-h-screen grid place-items-center p-4">
            <mat-card class="w-full max-w-lg border border-amber-300/40 bg-[#1a140c]/90 text-center">
                <mat-card-header class="!justify-center">
                    <mat-card-title class="!uppercase tracking-[0.12em]">Access Denied</mat-card-title>
                </mat-card-header>
                <mat-card-content>
                    <p class="text-amber-100/80">Your account does not have cache admin permissions.</p>
                </mat-card-content>
                <mat-card-actions class="!justify-center">
                    <a mat-stroked-button routerLink="/login">Return To Login</a>
                </mat-card-actions>
            </mat-card>
        </section>
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessDeniedPageComponent {}
