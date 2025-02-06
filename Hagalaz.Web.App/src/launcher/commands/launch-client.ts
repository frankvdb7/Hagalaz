import { Command } from "./command";
import { CommandType } from "../shared";

export class LaunchClientCommand implements Command<void> {
    commandType: CommandType = "launch-client";
}
