import { Component, ChangeDetectionStrategy, inject, resource, computed } from "@angular/core";
import { LauncherService } from "@app/launcher/launcher.service";
import { MatToolbar } from "@angular/material/toolbar";
import { MatButton } from "@angular/material/button";
import { MatIcon } from "@angular/material/icon";
import { HeaderComponent } from "@app/main/header/header.component";

@Component({
    selector: "app-launcher-header",
    templateUrl: "./header.component.html",
    styleUrls: ["./header.component.scss"],
    imports: [MatToolbar, MatButton, MatIcon, HeaderComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LauncherHeaderComponent {
    private launcherService = inject(LauncherService);
    private maximizedResource = resource({
        defaultValue: false,
        loader: () => this.launcherService.api.window.isMaximized(),
    });

    isWindowMaximized = computed(() => this.maximizedResource.value());

    onCloseClick() {
        this.launcherService.api.window.close();
    }

    onMaximizeClick() {
        this.launcherService.api.window.maximize();
        this.maximizedResource.reload();
    }

    onMinimizeClick() {
        this.launcherService.api.window.minimize();
    }
}
