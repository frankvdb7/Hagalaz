import { NgModule, inject } from "@angular/core";

import { MatIconRegistry } from "@angular/material/icon";
import { BASE_PATH } from "@app/config/assets";
import { DomSanitizer } from "@angular/platform-browser";

@NgModule()
export class LauncherHeaderIconModule {
    registry = inject(MatIconRegistry);
    sanitizer = inject(DomSanitizer);

    constructor() {
        const iconBasePath = `${BASE_PATH}icons/material/`;
        this.registry.addSvgIcon("window-close", this.sanitizer.bypassSecurityTrustResourceUrl(`${iconBasePath}window-close.svg`));
        this.registry.addSvgIcon("window-maximize", this.sanitizer.bypassSecurityTrustResourceUrl(`${iconBasePath}window-maximize.svg`));
        this.registry.addSvgIcon("window-minimize", this.sanitizer.bypassSecurityTrustResourceUrl(`${iconBasePath}window-minimize.svg`));
        this.registry.addSvgIcon("window-restore", this.sanitizer.bypassSecurityTrustResourceUrl(`${iconBasePath}window-restore.svg`));
    }
}
