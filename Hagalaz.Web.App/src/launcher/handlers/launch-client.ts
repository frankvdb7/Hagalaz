import { CommandHandler } from "./handler";
import { injectable } from "inversify";
import { CommandType } from "../shared";
import { shell, dialog } from "electron";
import * as process from "process";
import * as path from "path";
import * as fs from "fs";

@injectable()
export class LaunchClientHandler implements CommandHandler<void> {
    readonly commandType: CommandType = "launch-client";

    async handle(event: Electron.IpcMainEvent, ...args: any[]): Promise<void> {
        const workingDirectory = process.cwd();
        const fileLocation = "./hagalaz-client-1.0-SNAPSHOT/lib/hagalaz-client-1.0-SNAPSHOT.jar";
        const filePath = path.resolve(workingDirectory, fileLocation);
        if (!fs.existsSync(filePath)) {
            dialog.showErrorBox("Client not found", "The Hagalaz client could not be found");
            return;
        }
        await shell.openPath(filePath);
    }
}
