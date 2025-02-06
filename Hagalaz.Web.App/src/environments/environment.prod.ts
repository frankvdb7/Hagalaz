import { Environment, EnvironmentName } from "@environment/model";
import { prodEnvironment } from "@environment/common";

export const environment: Environment = {
    ...prodEnvironment,
    name: "web",
};
