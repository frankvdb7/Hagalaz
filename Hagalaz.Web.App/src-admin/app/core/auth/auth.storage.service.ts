import { Injectable } from "@angular/core";

const refreshTokenKey = "hagalaz.admin.refresh_token";

@Injectable({ providedIn: "root" })
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
