import { computed, Injectable, signal, inject } from "@angular/core";
import { firstValueFrom } from "rxjs";
import { AuthService } from "./auth.service";
import { AuthStorageService } from "./auth.storage.service";
import { AuthToken, UserInfo } from "./auth.models";

@Injectable({ providedIn: "root" })
export class AuthStore {
    private readonly authService = inject(AuthService);
    private readonly authStorage = inject(AuthStorageService);

    readonly loading = signal(false);
    readonly error = signal<string | null>(null);
    readonly token = signal<AuthToken | null>(null);
    readonly user = signal<UserInfo | null>(null);

    readonly authenticated = computed(() => this.token() !== null);
    readonly hasCacheScope = computed(() => this.token()?.scope.split(" ").includes("cache_api") ?? false);
    readonly isSystemAdministrator = computed(() => this.user()?.role?.includes("SystemAdministrator") ?? false);
    readonly isAuthorizedAdmin = computed(() => this.authenticated() && this.hasCacheScope() && this.isSystemAdministrator());

    async initialize(): Promise<void> {
        const refreshToken = this.authStorage.getRefreshToken();
        if (!refreshToken) {
            return;
        }

        try {
            this.loading.set(true);
            const token = await firstValueFrom(this.authService.refreshToken(refreshToken));
            this.setToken(token);
            await this.loadUser();
        } catch {
            this.clear();
        } finally {
            this.loading.set(false);
        }
    }

    async login(email: string, password: string, captcha: string): Promise<boolean> {
        this.error.set(null);

        try {
            this.loading.set(true);
            const captchaResult = await firstValueFrom(this.authService.verifyCaptcha(captcha));
            if (!captchaResult.succeeded) {
                this.error.set("Captcha verification failed.");
                return false;
            }

            const token = await firstValueFrom(this.authService.login(email, password));
            this.setToken(token);
            await this.loadUser();

            if (!this.isAuthorizedAdmin()) {
                this.error.set("User is authenticated but lacks required admin permissions.");
                return false;
            }

            return true;
        } catch {
            this.error.set("Authentication failed.");
            this.clear();
            return false;
        } finally {
            this.loading.set(false);
        }
    }

    async loadUser(): Promise<void> {
        const user = await firstValueFrom(this.authService.getUserInfo());
        this.user.set(user);
    }

    async logout(): Promise<void> {
        try {
            await firstValueFrom(this.authService.logout());
        } catch {
            // keep local logout behavior even if server logout fails
        }

        this.clear();
    }

    private setToken(token: AuthToken): void {
        this.authStorage.setRefreshToken(token.refresh_token);
        this.token.set(token);
    }

    private clear(): void {
        this.authStorage.removeRefreshToken();
        this.token.set(null);
        this.user.set(null);
    }
}
