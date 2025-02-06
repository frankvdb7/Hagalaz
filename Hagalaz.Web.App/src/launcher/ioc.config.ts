import "reflect-metadata";
import { Container } from "inversify";
import { LauncherApp } from "./launcher-app";
import { LauncherApiHandler } from "./launcher-api-handler";
import { COMMAND_HANDLER_TYPE, CommandHandler } from "./handlers/handler";
import { CloseWindowHandler } from "./handlers/close-window";
import { MaximizeWindowHandler } from "./handlers/maximize-window";
import { MinimizeWindowHandler } from "./handlers/minimize-window";
import { IsWindowMaximizedHandler } from "./handlers/is-window-maximized";
import { LaunchClientHandler } from "./handlers/launch-client";
import { ILogger, WinstonLogger } from "./logging/logger";

const launcherContainer = new Container();

/* logging */
launcherContainer.bind(ILogger).to(WinstonLogger).inSingletonScope();

/* main */
launcherContainer.bind(LauncherApp).toSelf().inSingletonScope();
launcherContainer.bind(LauncherApiHandler).toSelf().inSingletonScope();

/* command handlers */
launcherContainer
    .bind<CommandHandler>(COMMAND_HANDLER_TYPE)
    .to(CloseWindowHandler)
    .inTransientScope();
launcherContainer
    .bind<CommandHandler>(COMMAND_HANDLER_TYPE)
    .to(MaximizeWindowHandler)
    .inTransientScope();
launcherContainer
    .bind<CommandHandler>(COMMAND_HANDLER_TYPE)
    .to(MinimizeWindowHandler)
    .inTransientScope();
launcherContainer
    .bind<CommandHandler>(COMMAND_HANDLER_TYPE)
    .to(IsWindowMaximizedHandler)
    .inTransientScope();
launcherContainer
    .bind<CommandHandler>(COMMAND_HANDLER_TYPE)
    .to(LaunchClientHandler)
    .inTransientScope();

export { launcherContainer };
