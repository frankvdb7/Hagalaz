import type { CommandHandler } from "./handler";
import type { CommandType } from "../shared";
import { inject, injectable } from "inversify";
import { LauncherApp } from "../launcher-app";

@injectable()
export class IsWindowMaximizedHandler implements CommandHandler<boolean> {
    commandType: CommandType = "is-window-maximized";

    constructor(@inject(LauncherApp) private app: LauncherApp) {}

    async handle(_event: Electron.IpcMainEvent, ..._args: unknown[]) {
        return this.app.BrowserWindow.isMaximized();
    }
}
