import { Injectable } from "@angular/core";

@Injectable({
    providedIn: "root",
})
export class AuthStorageService {
    getRefreshToken(): string | null {
        return localStorage.getItem("refresh_token");
    }

    setRefreshToken(token: string): void {
        localStorage.setItem("refresh_token", token);
    }

    removeRefreshToken(): void {
        localStorage.removeItem("refresh_token");
    }
}
