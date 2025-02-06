import { CommandResultType, COMMANDS_CHANNEL, CommandType } from "./shared";
import { COMMAND_HANDLER_TYPE, CommandHandler } from "./handlers/handler";
import { ipcMain } from "electron";
import { Command } from "./commands/command";
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

    private getHandler(
        event: IpcMainEvent | IpcMainInvokeEvent,
        ...args: any[]
    ): CommandHandler | null {
        if (!args?.length) {
            this.logger.warn("Invalid launcher api arguments", event);
            return null;
        }
        const command: Command = args.shift();
        if (!("commandType" in command)) {
            this.logger.warn("Invalid launcher api command received", command);
            return null;
        }
        const handler = this._handlers.get(command.commandType);
        if (!handler) {
            this.logger.warn(
                "Invalid launcher api command type received",
                command.commandType
            );
            return null;
        }
        return handler;
    }

    private onCommandSend = async (event: IpcMainEvent, ...args: any[]) => {
        const handler = this.getHandler(event, ...args);
        if (!handler) {
            return;
        }
        try {
            await handler.handle(event, ...args);
        } catch (ex) {
            this.logger.error(
                `Failed to handle command '${handler.commandType}'`,
                ex
            );
        }
    };

    private onCommandInvoke = async (
        event: IpcMainInvokeEvent,
        ...args: any[]
    ): Promise<CommandResultType> => {
        const handler = this.getHandler(event, ...args);
        if (!handler) {
            return;
        }
        let result: CommandResultType;
        try {
            result = await handler.handle(event, ...args);
        } catch (ex) {
            this.logger.error(
                `Failed to invoke command '${handler.commandType}'`
            );
            return;
        }
        return result;
    };
}
