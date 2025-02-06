import { CommandResultType, CommandType } from "../shared";

export interface Command<TResult extends CommandResultType = any> {
    readonly commandType: CommandType;
}
