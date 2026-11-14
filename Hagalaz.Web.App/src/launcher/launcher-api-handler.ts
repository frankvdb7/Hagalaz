import { type CommandResultType, COMMANDS_CHANNEL, type CommandType } from "./shared";
import { COMMAND_HANDLER_TYPE, type CommandHandler } from "./handlers/handler";
import { ipcMain } from "electron";
import IpcMainEvent = Electron.IpcMainEvent;
import { inject, injectable, multiInject } from "inversify";
import IpcMainInvokeEvent = Electron.IpcMainInvokeEvent;
import { ILogger } from "./logging/logger";

/** The command type of an IPC command payload, or undefined when the payload is not a command. */
function commandTypeOf(command: unknown): string | undefined {
    const commandType = (command as { commandType?: unknown } | null | undefined)?.commandType;
    return typeof commandType === "string" ? commandType : undefined;
}

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
        if (!args.length) {
            this.logger.warn("Invalid launcher api arguments", event);
            return null;
        }
        const commandType = commandTypeOf(args[0]);
        if (commandType === undefined) {
            this.logger.warn("Invalid launcher api command received", args[0]);
            return null;
        }
        return this.handlerFor(commandType);
    }

    private handlerFor(commandType: string): CommandHandler | null {
        const handler = this._handlers.get(commandType as CommandType);
        if (!handler) this.logger.warn("Invalid launcher api command type received", commandType);
        return handler ?? null;
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
