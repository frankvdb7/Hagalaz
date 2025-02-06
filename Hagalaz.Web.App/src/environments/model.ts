export type EnvironmentName = "web" | "launcher";

export interface Environment {
    production: boolean;
    name: EnvironmentName;
    authApiUrl: string;
    characterApiUrl: string;
}
