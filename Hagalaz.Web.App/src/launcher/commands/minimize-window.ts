import { Command } from "./command";
import { CommandType } from "../shared";

export class MinimizeWindowCommand implements Command<void> {
    commandType: CommandType = "minimize-window";
}
