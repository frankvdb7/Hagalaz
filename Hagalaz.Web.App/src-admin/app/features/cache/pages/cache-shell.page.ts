import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTabsModule } from "@angular/material/tabs";
import { MatToolbarModule } from "@angular/material/toolbar";
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from "@angular/router";
import { AuthStore } from "../../../core/auth/auth.store";

@Component({
    standalone: true,
    selector: "cache-shell-page",
    imports: [RouterOutlet, RouterLink, RouterLinkActive, MatButtonModule, MatToolbarModule, MatIconModule, MatTabsModule],
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
