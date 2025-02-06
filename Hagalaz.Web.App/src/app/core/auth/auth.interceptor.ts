import { HttpEvent, HttpRequest, HttpHandlerFn } from "@angular/common/http";
import { inject } from "@angular/core";
import { Observable } from "rxjs";
import { AuthStore } from "./auth.store";

export function authInterceptor(request: HttpRequest<unknown>, next: HttpHandlerFn): Observable<HttpEvent<unknown>> {
    const store = inject(AuthStore);
    const token = store.token();
    if (token) {
        request = request.clone({
            setHeaders: {
                Authorization: `Bearer ${token.access_token}`,
            },
        });
    }
    return next(request);
}
