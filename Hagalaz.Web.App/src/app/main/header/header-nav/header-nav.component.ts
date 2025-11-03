import { Component, ChangeDetectionStrategy } from "@angular/core";
import { NavLink } from "@app/main/header/header.model";
import { RouterLink, RouterLinkActive } from "@angular/router";
import { MatTabLink, MatTabNav, MatTabNavPanel } from "@angular/material/tabs";

@Component({
    selector: "app-header-nav",
    templateUrl: "./header-nav.component.html",
    styleUrls: ["./header-nav.component.scss"],
    imports: [RouterLink, RouterLinkActive, MatTabNav, MatTabLink, MatTabNavPanel],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeaderNavComponent {
    links: NavLink[] = [
        {
            label: "News",
            url: "/news",
        },
        {
            label: "Highscores",
            url: "/highscores",
        },
        {
            label: "Shop",
            url: "/shop",
        },
        {
            label: "Play",
            url: "/play",
        },
    ];
}
