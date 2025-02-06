import { Command } from "./command";
import { CommandType } from "../shared";

export class MaximizeWindowCommand implements Command<void> {
    commandType: CommandType = "maximize-window";
}
