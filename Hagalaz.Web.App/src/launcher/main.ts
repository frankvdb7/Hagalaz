import { app, BrowserWindow } from "electron";
import { launcherContainer } from "./ioc.config";
import { LauncherApp } from "./launcher-app";
import { launcherBootstrap } from "./launcher-bootstrap";

// Handle creating/removing shortcuts on Windows when installing/uninstalling.
if (require("electron-squirrel-startup")) {
    app.quit();
}

async function createWindow() {
    await launcherBootstrap();

    const launcherApp = launcherContainer.get(LauncherApp);
    await launcherApp.openAsync();
}

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.on("ready", createWindow);

// Quit when all windows are closed, except on macOS. There, it's common
// for applications and their menu bar to stay active until the user quits
// explicitly with Cmd + Q.
app.on("window-all-closed", () => {
    if (process.platform !== "darwin") {
        app.quit();
    }
});

app.on("activate", async () => {
    // On OS X it's common to re-create a window in the app when the
    // dock icon is clicked and there are no other windows open.
    if (BrowserWindow.getAllWindows().length === 0) {
        await createWindow();
    }
});
