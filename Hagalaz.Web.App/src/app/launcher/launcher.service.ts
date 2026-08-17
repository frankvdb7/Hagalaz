import { Injectable } from "@angular/core";
import type { ILauncherApi } from "../../typings/launcher-api";

@Injectable({
    providedIn: "root",
})
export class LauncherService {
    private readonly launcherApiProxy: ILauncherApi;

    get api(): ILauncherApi {
        return this.launcherApiProxy;
    }

    constructor() {
        this.launcherApiProxy = new Proxy(window.launcherApi || {}, {}) as ILauncherApi;
    }
}
