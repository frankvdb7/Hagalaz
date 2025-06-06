export interface ILauncherApi {
    window: {
        isMaximized(): Promise<boolean>;
        minimize(): void;
        maximize(): void;
        close(): void;
    };
    client: {
        launch(): void;
    };
}

declare global {
    interface Window {
        launcherApi?: ILauncherApi;
    }
}
export {};
