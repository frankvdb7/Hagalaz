import { CommandType } from "../shared";
import { Command } from "./command";

export class IsWindowMaximized implements Command<boolean> {
    readonly commandType: CommandType = "is-window-maximized";
}
