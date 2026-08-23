import type { CommandResultType, CommandType } from "../shared";

export interface Command<_TResult extends CommandResultType = CommandResultType> {
    readonly commandType: CommandType;
}
