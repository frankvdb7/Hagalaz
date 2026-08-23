import type { CommandType } from "../shared";
import type { Command } from "./command";

export class IsWindowMaximized implements Command<boolean> {
    readonly commandType: CommandType = "is-window-maximized";
}
