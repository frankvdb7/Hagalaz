import { ChangeDetectionStrategy, Component, inject, computed } from "@angular/core";
import { Router, RouterOutlet, ActivatedRoute, NavigationEnd, RouterLink } from "@angular/router";
import { AuthStore } from "../../../core/auth/auth.store";
import { PortalSidebarComponent } from "../../../core/components/portal-sidebar/portal-sidebar.component";
import { MatIconModule } from "@angular/material/icon";
import { MatSidenavModule } from "@angular/material/sidenav";
import { MatMenuModule } from "@angular/material/menu";
import { MatButtonModule } from "@angular/material/button";
import { MatDividerModule } from "@angular/material/divider";
import { toSignal } from "@angular/core/rxjs-interop";
import { filter, map, startWith } from "rxjs/operators";

interface Breadcrumb {
    label: string;
    url: string;
}

@Component({
    standalone: true,
    selector: "cache-shell-page",
    imports: [
        RouterOutlet,
        RouterLink,
        PortalSidebarComponent,
        MatIconModule,
        MatSidenavModule,
        MatMenuModule,
        MatButtonModule,
        MatDividerModule
    ],
    templateUrl: "./cache-shell.page.html",
    styleUrl: "./cache-shell.page.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CacheShellPageComponent {
    readonly authStore = inject(AuthStore);
    private readonly router = inject(Router);
    private readonly activatedRoute = inject(ActivatedRoute);

    private readonly navigationEnd = toSignal(
        this.router.events.pipe(
            filter((event) => event instanceof NavigationEnd),
            startWith(null)
        )
    );

    readonly breadcrumbs = computed(() => {
        // Trigger re-computation on navigation end
        this.navigationEnd();
        return this.createBreadcrumbs(this.activatedRoute.root);
    });

    async logout(): Promise<void> {
        await this.authStore.logout();
        await this.router.navigate(["/login"]);
    }

    private createBreadcrumbs(route: ActivatedRoute, url: string = "", breadcrumbs: Breadcrumb[] = []): Breadcrumb[] {
        const children: ActivatedRoute[] = route.children;

        if (children.length === 0) {
            return breadcrumbs;
        }

        for (const child of children) {
            const routeURL: string = child.snapshot.url.map((segment) => segment.path).join("/");
            if (routeURL !== "") {
                url += `/${routeURL}`;
            }

            const label = child.snapshot.data["breadcrumb"];
            if (label) {
                breadcrumbs.push({ label, url });
            }

            return this.createBreadcrumbs(child, url, breadcrumbs);
        }

        return breadcrumbs;
    }
}
