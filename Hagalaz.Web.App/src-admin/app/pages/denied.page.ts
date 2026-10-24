import { ChangeDetectionStrategy, Component } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { RouterLink } from "@angular/router";

@Component({
    standalone: true,
    selector: "admin-denied-page",
    imports: [RouterLink, MatButtonModule, MatCardModule],
    template: `
        <section class="min-h-screen grid place-items-center p-4" aria-labelledby="denied-title">
            <mat-card appearance="outlined" class="w-full max-w-md">
                <mat-card-header>
                    <mat-card-title><h1 id="denied-title">Access denied</h1></mat-card-title>
                </mat-card-header>
                <mat-card-content>
                    <p>Your account does not have cache administration access. Ask a system administrator to check your permissions, or sign in with a staff account.</p>
                </mat-card-content>
                <mat-card-actions align="end">
                    <a mat-button routerLink="/login">Back to sign in</a>
                </mat-card-actions>
            </mat-card>
        </section>
    `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccessDeniedPageComponent {}
