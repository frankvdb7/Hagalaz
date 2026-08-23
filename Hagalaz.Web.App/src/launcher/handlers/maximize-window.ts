import { inject, injectable } from "inversify";
import type { CommandHandler } from "./handler";
import type { CommandType } from "../shared";
import { LauncherApp } from "../launcher-app";

@injectable()
export class MaximizeWindowHandler implements CommandHandler<undefined> {
    commandType: CommandType = "maximize-window";

    constructor(@inject(LauncherApp) private app: LauncherApp) {}

    async handle(_event: Electron.IpcMainEvent, ..._args: unknown[]): Promise<undefined> {
        const window = this.app.BrowserWindow;
        if (!window.isMaximized()) {
            this.app.BrowserWindow.maximize();
        } else {
            this.app.BrowserWindow.restore();
        }
        return undefined;
    }
}
