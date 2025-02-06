import { Environment } from "@environment/model";

export const devEnvironment: Omit<Environment, "name"> = {
    production: false,
    authApiUrl: "/rdx/authorization/",
    characterApiUrl: "/rdx/characters/api/v1/",
};

export const prodEnvironment: Omit<Environment, "name"> = {
    production: true,
    authApiUrl: "/rdx/authorization/",
    characterApiUrl: "/rdx/characters/api/v1/",
};
