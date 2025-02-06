import { CommandResultType, CommandType } from "../shared";
import IpcMainEvent = Electron.IpcMainEvent;
import IpcMainInvokeEvent = Electron.IpcMainInvokeEvent;

export const COMMAND_HANDLER_TYPE = "CommandHandler";

export interface CommandHandler<TResult extends CommandResultType = any> {
    readonly commandType: CommandType;

    handle(
        event: IpcMainEvent | IpcMainInvokeEvent,
        ...args: any[]
    ): Promise<TResult>;
}
