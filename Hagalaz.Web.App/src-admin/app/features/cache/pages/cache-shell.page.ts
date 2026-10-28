import { Component } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import { RouterOutlet } from "@angular/router";

@Component({
    selector: "cache-shell-page",
    imports: [MatIconModule, RouterOutlet],
    templateUrl: "./cache-shell.page.html",
    styleUrl: "./cache-shell.page.scss",
})
export class CacheShellPageComponent {}
