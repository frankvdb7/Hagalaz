import { Injectable, inject } from "@angular/core";
import { HttpClient, HttpHeaders, HttpParams } from "@angular/common/http";
import { AuthLoginRequest, AuthLoginResponse, AuthToken } from "./auth.models";
import { Observable } from "rxjs";
import { environment } from "@environment/environment";
import { Result } from "@app/services/models";

@Injectable({
    providedIn: "root",
})
export class AuthService {
    private http = inject(HttpClient);

    login(auth: AuthLoginRequest): Observable<AuthLoginResponse> {
        return this.getTokens(
            {
                username: auth.email,
                password: auth.password,
            },
            "password"
        );
    }

    logout(): Observable<any> {
        return this.http.get(`${environment.authApiUrl}connect/logout`);
    }

    private getTokens(
        data: { username: string; password: string } | { refresh_token: string },
        grantType: "password" | "refresh_token"
    ): Observable<AuthToken> {
        const options = {
            headers: new HttpHeaders({
                "Content-Type": "application/x-www-form-urlencoded",
            }),
        };
        Object.assign(data, {
            grant_type: grantType,
            scope: "openid offline_access email profile roles characters_api",
            client_id: "storm-app",
        });
        const body = Object.entries(data).reduce(
            // @ts-ignore
            (params, [key, value]) => params.set(key, data[key]),
            new HttpParams()
        );

        return this.http.post<AuthToken>(`${environment.authApiUrl}connect/token`, body, options);
    }

    refreshToken(token: string): Observable<AuthToken> {
        return this.getTokens({ refresh_token: token }, "refresh_token");
    }

    verifyCaptcha(token: string): Observable<Result> {
        return this.http.post<Result>(`${environment.authApiUrl}captcha/verify`, {
            token,
        });
    }
}
