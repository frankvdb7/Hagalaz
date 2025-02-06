import { Command } from "./command";
import { CommandType } from "../shared";

export class CloseWindowCommand implements Command<void> {
    commandType: CommandType = "close-window";
}
