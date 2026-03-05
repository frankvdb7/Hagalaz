import { CanActivateFn, Router } from "@angular/router";
import { inject } from "@angular/core";
import { AuthStore } from "../auth/auth.store";

export const adminCanActivate: CanActivateFn = () => {
    const store = inject(AuthStore);
    const router = inject(Router);

    if (!store.authenticated()) {
        return router.createUrlTree(["/login"]);
    }

    if (!store.isAuthorizedAdmin()) {
        return router.createUrlTree(["/denied"]);
    }

    return true;
};
