import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { RouterLinkActive, RouterLink, RouterOutlet } from "@angular/router";
import { MatCard } from "@angular/material/card";
import { MatAnchor } from "@angular/material/button";
import { MatDivider } from "@angular/material/divider";
import { UserStore } from "@app/core/user/user.store";

@Component({
    selector: "app-profile",
    templateUrl: "./profile.component.html",
    styleUrls: ["./profile.component.scss"],
    host: { class: "flex flex-auto p-4" },
    imports: [MatCard, RouterOutlet, RouterLink, RouterLinkActive, MatAnchor, MatDivider],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileComponent {
    store = inject(UserStore);

    links = [
        {
            label: "Overview",
            url: "/profile/overview",
        },
        {
            label: "Player vs Player",
            url: "/profile/pvp",
        },
    ];
}
