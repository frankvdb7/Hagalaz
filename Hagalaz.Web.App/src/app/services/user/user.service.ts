import { inject, Service } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { environment } from "@environment/environment";
import type { UserInfo } from "@app/services/user/user.model";

@Service()
export class UserService {
    private http = inject(HttpClient);

    getUserInfo() {
        return this.http.get<UserInfo>(`${environment.authApiUrl}connect/userinfo`);
    }
}
