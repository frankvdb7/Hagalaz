import type { CommandHandler } from "./handler";
import { injectable } from "inversify";
import type { CommandType } from "../shared";
import { shell, dialog } from "electron";
import * as process from "node:process";
import * as path from "node:path";
import * as fs from "node:fs";

@injectable()
export class LaunchClientHandler implements CommandHandler<undefined> {
    readonly commandType: CommandType = "launch-client";

    async handle(_event: Electron.IpcMainEvent, ..._args: unknown[]): Promise<undefined> {
        const workingDirectory = process.cwd();
        const fileLocation = "./hagalaz-client-1.0-SNAPSHOT/lib/hagalaz-client-1.0-SNAPSHOT.jar";
        const filePath = path.resolve(workingDirectory, fileLocation);
        if (!fs.existsSync(filePath)) {
            dialog.showErrorBox("Client not found", "The Hagalaz client could not be found");
            return undefined;
        }
        await shell.openPath(filePath);
        return undefined;
    }
}
