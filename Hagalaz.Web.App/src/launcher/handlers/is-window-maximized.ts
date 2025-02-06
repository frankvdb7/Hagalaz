import { CommandHandler } from "./handler";
import { CommandType } from "../shared";
import { inject, injectable } from "inversify";
import { LauncherApp } from "../launcher-app";

@injectable()
export class IsWindowMaximizedHandler implements CommandHandler<boolean> {
    commandType: CommandType = "is-window-maximized";

    constructor(@inject(LauncherApp) private app: LauncherApp) {}

    async handle(event: Electron.IpcMainEvent, ...args: any[]) {
        return this.app.BrowserWindow.isMaximized();
    }
}
