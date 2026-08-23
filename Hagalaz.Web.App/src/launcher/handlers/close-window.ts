import type { CommandHandler } from "./handler";
import type { CommandType } from "../shared";
import { inject, injectable } from "inversify";
import { LauncherApp } from "../launcher-app";

@injectable()
export class CloseWindowHandler implements CommandHandler<undefined> {
    commandType: CommandType = "close-window";

    constructor(@inject(LauncherApp) private app: LauncherApp) {}

    async handle(_event: Electron.IpcMainEvent, ..._args: unknown[]): Promise<undefined> {
        this.app.BrowserWindow.close();
        return undefined;
    }
}
