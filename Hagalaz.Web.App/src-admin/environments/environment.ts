export interface AdminEnvironment {
    production: boolean;
    authApiUrl: string;
    cacheApiUrl: string;
}

export const environment: AdminEnvironment = {
    production: false,
    authApiUrl: "/rdx/authorization/",
    cacheApiUrl: "/rdx/cache/api/v1/cache/",
};
