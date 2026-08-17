import type { Command } from "./command";
import type { CommandType } from "../shared";

export class MaximizeWindowCommand implements Command<undefined> {
    commandType: CommandType = "maximize-window";
}
