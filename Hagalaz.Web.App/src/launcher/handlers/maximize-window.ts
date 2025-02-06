import { inject, injectable } from "inversify";
import { CommandHandler } from "./handler";
import { CommandType } from "../shared";
import { LauncherApp } from "../launcher-app";

@injectable()
export class MaximizeWindowHandler implements CommandHandler<void> {
    commandType: CommandType = "maximize-window";

    constructor(@inject(LauncherApp) private app: LauncherApp) {}

    async handle(event: Electron.IpcMainEvent, ...args: any[]) {
        const window = this.app.BrowserWindow;
        if (!window.isMaximized()) {
            this.app.BrowserWindow.maximize();
        } else {
            this.app.BrowserWindow.restore();
        }
    }
}
