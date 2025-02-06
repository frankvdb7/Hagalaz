import { inject, injectable } from "inversify";
import { CommandHandler } from "./handler";
import { CommandType } from "../shared";
import { LauncherApp } from "../launcher-app";

@injectable()
export class MinimizeWindowHandler implements CommandHandler<void> {
    commandType: CommandType = "minimize-window";

    constructor(@inject(LauncherApp) private app: LauncherApp) {}

    async handle(event: Electron.IpcMainEvent, ...args: any[]) {
        this.app.BrowserWindow.minimize();
    }
}
