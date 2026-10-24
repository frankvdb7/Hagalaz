import { Service } from "@angular/core";

const refreshTokenKey = "hagalaz.admin.refresh_token";

@Service()
export class AuthStorageService {
    getRefreshToken(): string | null {
        return localStorage.getItem(refreshTokenKey);
    }

    setRefreshToken(token: string): void {
        localStorage.setItem(refreshTokenKey, token);
    }

    removeRefreshToken(): void {
        localStorage.removeItem(refreshTokenKey);
    }
}
