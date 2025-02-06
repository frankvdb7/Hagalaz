import { Component, OnInit, ChangeDetectionStrategy, inject } from "@angular/core";
import { LauncherService } from "@app/launcher/launcher.service";
import { MatToolbar } from "@angular/material/toolbar";
import { MatButton } from "@angular/material/button";
import { MatIcon } from "@angular/material/icon";
import { HeaderComponent } from "@app/main/header/header.component";
import { LauncherHeaderIconModule } from "./header-icon.module";

@Component({
    selector: "app-launcher-header",
    templateUrl: "./header.component.html",
    styleUrls: ["./header.component.scss"],
    imports: [MatToolbar, MatButton, MatIcon, HeaderComponent, LauncherHeaderIconModule],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LauncherHeaderComponent implements OnInit {
    private launcherService = inject(LauncherService);

    isWindowMaximized: boolean | undefined;

    async ngOnInit() {
        await this.refreshMaximizeRestore();
    }

    onCloseClick() {
        this.launcherService.api.window.close();
    }

    async onMaximizeClick() {
        this.launcherService.api.window.maximize();
        await this.refreshMaximizeRestore();
    }

    onMinimizeClick() {
        this.launcherService.api.window.minimize();
    }

    async refreshMaximizeRestore() {
        this.isWindowMaximized = await this.launcherService.api.window.isMaximized();
    }
}
