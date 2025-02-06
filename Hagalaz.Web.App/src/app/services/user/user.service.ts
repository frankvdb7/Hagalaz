import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { environment } from "@environment/environment";
import { UserInfo } from "@app/services/user/user.model";

@Injectable({
    providedIn: "root",
})
export class UserService {
    private http = inject(HttpClient);

    getUserInfo() {
        return this.http.get<UserInfo>(`${environment.authApiUrl}connect/userinfo`);
    }
}
