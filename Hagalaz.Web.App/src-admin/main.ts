import { provideHttpClient, withFetch, withInterceptors } from "@angular/common/http";
import { provideAppInitializer, provideZonelessChangeDetection, inject, importProvidersFrom } from "@angular/core";
import { MatIconRegistry } from "@angular/material/icon";
import { bootstrapApplication } from "@angular/platform-browser";
import { provideRouter } from "@angular/router";
import { NgHcaptchaModule } from "ng-hcaptcha";
import { AdminAppComponent } from "./app/app.component";
import { routes } from "./app/app.routes";
import { authInterceptor } from "./app/core/auth/auth.interceptor";
import { AuthStore } from "./app/core/auth/auth.store";
import { environment } from "./environments/environment";

bootstrapApplication(AdminAppComponent, {
    providers: [
        provideRouter(routes),
        provideZonelessChangeDetection(),
        provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
        importProvidersFrom(
            NgHcaptchaModule.forRoot({
                siteKey:
                    !window.location.hostname.includes("localhost") && environment.production
                        ? "98335e9b-d163-4a4a-9a12-80d040a3b7d8"
                        : "10000000-ffff-ffff-ffff-000000000001",
            })
        ),
        provideAppInitializer(() => {
            const iconRegistry = inject(MatIconRegistry);
            iconRegistry.setDefaultFontSetClass("material-symbols");
        }),
        provideAppInitializer(() => inject(AuthStore).initialize()),
    ],
}).catch((err) => console.error(err));
