import { CommandHandler } from "./handler";
import { CommandType } from "../shared";
import { inject, injectable } from "inversify";
import { LauncherApp } from "../launcher-app";

@injectable()
export class CloseWindowHandler implements CommandHandler<void> {
    commandType: CommandType = "close-window";

    constructor(@inject(LauncherApp) private app: LauncherApp) {}

    async handle(event: Electron.IpcMainEvent, ...args: any[]) {
        this.app.BrowserWindow.close();
    }
}
