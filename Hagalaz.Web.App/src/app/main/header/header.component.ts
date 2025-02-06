import { Component, ChangeDetectionStrategy, inject, Signal } from "@angular/core";
import { LoadingBarService } from "@ngx-loading-bar/core";
import { BreakpointObserver, Breakpoints } from "@angular/cdk/layout";
import { map } from "rxjs/operators";
import { MatIcon } from "@angular/material/icon";
import { MatToolbar, MatToolbarRow } from "@angular/material/toolbar";
import { MatDivider } from "@angular/material/divider";
import { MatMenu, MatMenuContent, MatMenuItem, MatMenuTrigger } from "@angular/material/menu";
import { NgClass } from "@angular/common";
import { HeaderNavComponent } from "./header-nav/header-nav.component";
import { SvgHttpLoader, SvgLoader, SVG_ICON_REGISTRY_PROVIDER, SvgIconComponent } from "angular-svg-icon";
import { MatProgressBar } from "@angular/material/progress-bar";
import { MatAnchor, MatIconButton } from "@angular/material/button";
import { RouterLink } from "@angular/router";
import { AuthStore } from "@app/core/auth/auth.store";
import { UserStore } from "@app/core/user/user.store";
import { toSignal } from "@angular/core/rxjs-interop";

@Component({
    selector: "app-header",
    templateUrl: "./header.component.html",
    styleUrls: ["./header.component.scss"],
    providers: [SVG_ICON_REGISTRY_PROVIDER, { provide: SvgLoader, useClass: SvgHttpLoader }],
    imports: [
        NgClass,
        RouterLink,
        MatIcon,
        MatToolbar,
        MatToolbarRow,
        MatDivider,
        MatMenu,
        MatMenuItem,
        MatMenuContent,
        MatMenuTrigger,
        HeaderNavComponent,
        SvgIconComponent,
        MatProgressBar,
        MatAnchor,
        MatIconButton,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class HeaderComponent {
    private breakpointObserver = inject(BreakpointObserver);
    private loader = inject(LoadingBarService);

    authStore = inject(AuthStore);
    userStore = inject(UserStore);

    smallScreen = toSignal(this.breakpointObserver.observe([Breakpoints.XSmall, Breakpoints.Small]).pipe(map((state) => state.matches)));
    logoAnimationClass: string | undefined;
    loadValue = toSignal(this.loader.value$);

    playLogoAnimation() {
        this.logoAnimationClass = "play";
    }

    stopLogoAnimation() {
        this.logoAnimationClass = "";
    }

    logout() {
        this.authStore.logout();
    }
}
