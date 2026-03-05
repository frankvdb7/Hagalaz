import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from "@angular/router";
import { MatButtonModule } from "@angular/material/button";
import { MatToolbarModule } from "@angular/material/toolbar";
import { AuthStore } from "../../../core/auth/auth.store";

@Component({
    standalone: true,
    selector: "cache-shell-page",
    imports: [RouterOutlet, RouterLink, RouterLinkActive, MatButtonModule, MatToolbarModule],
    templateUrl: "./cache-shell.page.html",
    styleUrl: "./cache-shell.page.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CacheShellPageComponent {
    readonly authStore = inject(AuthStore);
    private readonly router = inject(Router);

    async logout(): Promise<void> {
        await this.authStore.logout();
        await this.router.navigate(["/login"]);
    }
}
