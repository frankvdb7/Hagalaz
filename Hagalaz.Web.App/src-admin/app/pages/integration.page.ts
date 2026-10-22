import { Component, inject } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatIconModule } from "@angular/material/icon";
import { ActivatedRoute, RouterLink } from "@angular/router";

@Component({
    selector: "admin-integration-page",
    imports: [RouterLink, MatButtonModule, MatCardModule, MatChipsModule, MatIconModule],
    template: `
        <section aria-labelledby="integration-title" class="grid gap-6">
            <header><p class="eyebrow mb-3">{{ page.eyebrow }}</p><h1 id="integration-title">{{ page.title }}</h1><p class="supporting-text mt-2">{{ page.description }}</p></header>
            <div class="notice flex items-start gap-3"><mat-icon class="shrink-0">info</mat-icon><p>{{ page.notice }}</p></div>
            <div class="grid gap-6 lg:grid-cols-3">
                @for (capability of page.capabilities; track capability.title) {
                    <mat-card appearance="outlined"><mat-card-content class="flex flex-col gap-5 pt-6">
                        <span class="feature-icon" [class]="accents[capability.icon] ?? ''"><mat-icon>{{ capability.icon }}</mat-icon></span><h2>{{ capability.title }}</h2>
                        <mat-chip-set aria-label="Integration status"><mat-chip>Not connected</mat-chip></mat-chip-set>
                        <p class="supporting-text">{{ capability.description }}</p>
                    </mat-card-content></mat-card>
                }
            </div>
            <mat-card appearance="outlined"><mat-card-header><mat-card-title><h2>Available now</h2></mat-card-title></mat-card-header><mat-card-content><p class="supporting-text">Cache Studio is connected to the existing cache API for definition inspection, supported field updates, and sprite editing.</p><a mat-flat-button href="/cache/types" routerLink="/cache/types" class="mt-5"><mat-icon>database</mat-icon>Open Cache Studio</a></mat-card-content></mat-card>
        </section>
    `,
})
export class IntegrationPageComponent {
    readonly accents: Record<string, string> = {
        group: "accent-cyan",
        public: "accent-cyan",
        dns: "accent-cyan",
        swap_horiz: "accent-cyan",
        monitoring: "accent-violet",
        history: "accent-violet",
        settings: "accent-violet",
        manage_accounts: "accent-emerald",
        verified_user: "accent-emerald",
        shield: "accent-emerald",
        warning: "accent-ember",
        policy: "accent-ember",
    };
    readonly page = inject(ActivatedRoute).snapshot.data as {
        title: string;
        eyebrow: string;
        description: string;
        notice: string;
        capabilities: { title: string; icon: string; description: string }[];
    };
}
