import { BreakpointObserver } from "@angular/cdk/layout";
import { NgOptimizedImage, NgTemplateOutlet } from "@angular/common";
import { Component, computed, inject, signal } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { FormField, form } from "@angular/forms/signals";
import { MatAutocompleteModule } from "@angular/material/autocomplete";
import { MatButtonModule } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatListModule } from "@angular/material/list";
import { MatMenuModule } from "@angular/material/menu";
import { MatSidenavModule } from "@angular/material/sidenav";
import { MatToolbarModule } from "@angular/material/toolbar";
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from "@angular/router";
import { filter } from "rxjs";
import { AuthStore } from "../core/auth/auth.store";
import { cacheTypeNames, type TypeKind } from "../features/cache/services/cache.models";
import { PortalBreadcrumb } from "./portal-breadcrumb";
import { portalNavigation } from "./portal-navigation";

@Component({
    selector: "admin-portal-shell",
    imports: [
        NgOptimizedImage,
        NgTemplateOutlet,
        RouterLink,
        RouterLinkActive,
        RouterOutlet,
        FormField,
        MatAutocompleteModule,
        MatButtonModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatListModule,
        MatMenuModule,
        MatSidenavModule,
        MatToolbarModule,
    ],
    providers: [PortalBreadcrumb],
    templateUrl: "./portal-shell.page.html",
    styleUrl: "./portal-shell.page.scss",
    host: { class: "admin-portal" },
})
export class PortalShellPageComponent {
    readonly breadcrumb = inject(PortalBreadcrumb);
    readonly auth = inject(AuthStore);
    private readonly router = inject(Router);
    readonly navigation = portalNavigation;
    private readonly searchModel = signal({ query: "" });
    readonly searchForm = form(this.searchModel);
    readonly searchDestinations = computed(() => {
        const query = this.searchModel().query.trim().toLowerCase();
        return [...this.navigation.flatMap((group) => group.links), { title: "Sprites", path: "/cache/types?kind=sprites", icon: "image" }].filter((link) =>
            link.title.toLowerCase().includes(query)
        );
    });
    readonly initials = computed(() =>
        (this.auth.user()?.preferred_username ?? "Administrator")
            .trim()
            .split(/[\s._-]+/)
            .slice(0, 2)
            .map((part) => part.charAt(0))
            .join("")
            .toUpperCase()
    );
    private readonly breakpoint = toSignal(inject(BreakpointObserver).observe("(max-width: 959px)"));
    readonly compact = computed(() => this.breakpoint()?.matches ?? false);
    private readonly navigationEnd = toSignal(this.router.events.pipe(filter((event) => event instanceof NavigationEnd)));
    readonly currentUrl = computed(() => (this.navigationEnd()?.urlAfterRedirects ?? this.router.url).split("#")[0]);
    readonly definitionCrumb = computed(() => {
        const match = this.currentUrl().match(/^\/cache\/types\/([^/]+)\/(\d+)(?:[/?]|$)/);
        if (!match || !Object.hasOwn(cacheTypeNames, match[1])) return null;
        const kind = match[1] as TypeKind;
        return { kind, label: cacheTypeNames[kind], id: match[2] };
    });
    readonly title = computed(() => {
        const url = this.currentUrl();
        return this.navigation.flatMap((group) => group.links).find((link) => url.startsWith(link.path))?.title ?? "Workspace";
    });

    async openDestination(path: string): Promise<void> {
        this.searchModel.set({ query: "" });
        await this.router.navigateByUrl(path);
    }

    async logout(): Promise<void> {
        if ((await this.router.navigate(["/login"])) && this.router.url === "/login") await this.auth.logout();
    }
}
