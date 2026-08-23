import { contextBridge, ipcRenderer } from "electron";
import type { ILauncherApi } from "../typings/launcher-api";
import { type CommandResultType, COMMANDS_CHANNEL } from "./shared";
import { CloseWindowCommand } from "./commands/close-window";
import type { Command } from "./commands/command";
import { MaximizeWindowCommand } from "./commands/maximize-window";
import { MinimizeWindowCommand } from "./commands/minimize-window";
import { IsWindowMaximized } from "./commands/is-window-maximized";
import { LaunchClientCommand } from "./commands/launch-client";

function sendCommand(command: Command<undefined>) {
    ipcRenderer.send(COMMANDS_CHANNEL, command);
}

async function invokeCommand<TResult extends CommandResultType>(command: Command<TResult>) {
    const result = await ipcRenderer.invoke(COMMANDS_CHANNEL, command);
    return result as TResult;
}

const launcherApi: ILauncherApi = {
    window: {
        isMaximized() {
            return invokeCommand(new IsWindowMaximized());
        },
        minimize() {
            sendCommand(new MinimizeWindowCommand());
        },
        maximize() {
            sendCommand(new MaximizeWindowCommand());
        },
        close() {
            sendCommand(new CloseWindowCommand());
        },
    },
    client: {
        launch() {
            sendCommand(new LaunchClientCommand());
        },
    },
};

contextBridge.exposeInMainWorld("launcherApi", launcherApi);
