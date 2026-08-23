import type { Command } from "./command";
import type { CommandType } from "../shared";

export class LaunchClientCommand implements Command<undefined> {
    commandType: CommandType = "launch-client";
}
