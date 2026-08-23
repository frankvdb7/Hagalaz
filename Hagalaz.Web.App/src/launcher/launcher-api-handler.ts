import { type CommandResultType, COMMANDS_CHANNEL, type CommandType } from "./shared";
import { COMMAND_HANDLER_TYPE, type CommandHandler } from "./handlers/handler";
import { ipcMain } from "electron";
import IpcMainEvent = Electron.IpcMainEvent;
import { inject, injectable, multiInject } from "inversify";
import IpcMainInvokeEvent = Electron.IpcMainInvokeEvent;
import { ILogger } from "./logging/logger";

@injectable()
export class LauncherApiHandler {
    private _handlers = new Map<CommandType, CommandHandler>();

    constructor(
        @inject(ILogger) private logger: ILogger,
        @multiInject(COMMAND_HANDLER_TYPE)
        handlers: CommandHandler[]
    ) {
        handlers.forEach((handler) => {
            this._handlers.set(handler.commandType, handler);
        });
        ipcMain.on(COMMANDS_CHANNEL, this.onCommandSend);
        ipcMain.handle(COMMANDS_CHANNEL, this.onCommandInvoke);
    }

    private getHandler(event: IpcMainEvent | IpcMainInvokeEvent, ...args: unknown[]): CommandHandler | null {
        if (!args?.length) {
            this.logger.warn("Invalid launcher api arguments", event);
            return null;
        }
        const command = args.shift();
        if (typeof command !== "object" || command === null || !("commandType" in command) || typeof command.commandType !== "string") {
            this.logger.warn("Invalid launcher api command received", command);
            return null;
        }
        const handler = this._handlers.get(command.commandType as CommandType);
        if (!handler) {
            this.logger.warn("Invalid launcher api command type received", command.commandType);
            return null;
        }
        return handler;
    }

    private onCommandSend = async (event: IpcMainEvent, ...args: unknown[]) => {
        const handler = this.getHandler(event, ...args);
        if (!handler) {
            return;
        }
        try {
            await handler.handle(event, ...args);
        } catch (ex) {
            this.logger.error(`Failed to handle command '${handler.commandType}'`, ex);
        }
    };

    private onCommandInvoke = async (event: IpcMainInvokeEvent, ...args: unknown[]): Promise<CommandResultType> => {
        const handler = this.getHandler(event, ...args);
        if (!handler) {
            return;
        }
        let result: CommandResultType;
        try {
            result = await handler.handle(event, ...args);
        } catch (_ex) {
            this.logger.error(`Failed to invoke command '${handler.commandType}'`);
            return;
        }
        return result;
    };
}
