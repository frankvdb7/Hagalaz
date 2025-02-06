import { inject, injectable } from "inversify";
import { BrowserWindow } from "electron";
import * as path from "path";

@injectable()
export class LauncherApp {
    private _mainWindow!: BrowserWindow;

    get BrowserWindow() {
        return this._mainWindow;
    }

    async openAsync() {
        if (this._mainWindow) {
            throw new Error("Browser window is already open");
        }
        this._mainWindow = new BrowserWindow({
            show: false,
            frame: false,
            minWidth: 1000,
            minHeight: 640,
            titleBarStyle: "hidden",
            backgroundColor: "#FFF",
            webPreferences: {
                preload: path.join(__dirname, "preload.js"),
            },
        });
        this._mainWindow.center();

        this._mainWindow.once("ready-to-show", () => {
            this._mainWindow.show();
        });

        this._mainWindow.webContents.openDevTools();
        await this._mainWindow.loadURL("http://localhost:4200/");
    }
}
