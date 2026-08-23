import type { Command } from "./command";
import type { CommandType } from "../shared";

export class CloseWindowCommand implements Command<undefined> {
    commandType: CommandType = "close-window";
}
