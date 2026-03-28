import { HttpClient, HttpHeaders, HttpParams } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { AuthToken, ResultDto, UserInfo } from "./auth.models";

@Injectable({ providedIn: "root" })
export class AuthService {
    private readonly http = inject(HttpClient);

    login(email: string, password: string): Observable<AuthToken> {
        return this.getTokens({ username: email, password }, "password");
    }

    refreshToken(refreshToken: string): Observable<AuthToken> {
        return this.getTokens({ refresh_token: refreshToken }, "refresh_token");
    }

    verifyCaptcha(token: string): Observable<ResultDto> {
        return this.http.post<ResultDto>(`${environment.authApiUrl}captcha/verify`, { token });
    }

    getUserInfo(): Observable<UserInfo> {
        return this.http.get<UserInfo>(`${environment.authApiUrl}connect/userinfo`);
    }

    logout(): Observable<unknown> {
        return this.http.get(`${environment.authApiUrl}connect/logout`);
    }

    private getTokens(data: { username: string; password: string } | { refresh_token: string }, grantType: "password" | "refresh_token"): Observable<AuthToken> {
        const options = {
            headers: new HttpHeaders({
                "Content-Type": "application/x-www-form-urlencoded",
            }),
        };

        Object.assign(data, {
            grant_type: grantType,
            scope: "openid offline_access email profile roles characters_api cache_api",
            client_id: "storm-app",
        });

        const body = Object.entries(data).reduce((params, [key, value]) => params.set(key, value as string), new HttpParams());

        return this.http.post<AuthToken>(`${environment.authApiUrl}connect/token`, body, options);
    }
}
