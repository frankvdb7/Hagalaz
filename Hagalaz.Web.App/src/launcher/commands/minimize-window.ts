import type { Command } from "./command";
import type { CommandType } from "../shared";

export class MinimizeWindowCommand implements Command<undefined> {
    commandType: CommandType = "minimize-window";
}
