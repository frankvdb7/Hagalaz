import { inject, injectable } from "inversify";
import type { CommandHandler } from "./handler";
import type { CommandType } from "../shared";
import { LauncherApp } from "../launcher-app";

@injectable()
export class MinimizeWindowHandler implements CommandHandler<undefined> {
    commandType: CommandType = "minimize-window";

    constructor(@inject(LauncherApp) private app: LauncherApp) {}

    async handle(_event: Electron.IpcMainEvent, ..._args: unknown[]): Promise<undefined> {
        this.app.BrowserWindow.minimize();
        return undefined;
    }
}
