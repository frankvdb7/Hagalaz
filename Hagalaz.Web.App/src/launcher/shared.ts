export const COMMANDS_CHANNEL = "commands";
export type CommandType =
    | "maximize-window"
    | "minimize-window"
    | "close-window"
    | "is-window-maximized"
    | "launch-client";
export type CommandResultType = void | boolean | number | string;
