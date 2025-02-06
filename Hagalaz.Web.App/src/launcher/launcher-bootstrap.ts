import { launcherContainer } from "./ioc.config";
import { LauncherApiHandler } from "./launcher-api-handler";

export async function launcherBootstrap() {
    launcherContainer.get(LauncherApiHandler);
}
