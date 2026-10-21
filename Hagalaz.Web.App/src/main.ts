import { provideHttpClient, withInterceptors, withInterceptorsFromDi } from "@angular/common/http";

import { enableProdMode, importProvidersFrom, inject, provideAppInitializer, provideZonelessChangeDetection } from "@angular/core";
import { MatIconRegistry } from "@angular/material/icon";
import { bootstrapApplication } from "@angular/platform-browser";
import {
    provideRouter,
    TitleStrategy,
    withAutoCleanupInjectors,
    withComponentInputBinding,
    withExperimentalPlatformNavigation,
    withViewTransitions,
} from "@angular/router";
import { provideServiceWorker } from "@angular/service-worker";
import { AppComponent } from "@app/app.component";
import { getAppRoutes } from "@app/app-routing";
import { initAuthStore } from "@app/core/auth/auth.init";
import { authInterceptor } from "@app/core/auth/auth.interceptor";
import { TemplatePageTitleStrategy } from "@app/core/router/template-page-title.strategy";
import { environment } from "@environment/environment";
import { provideLoadingBarInterceptor } from "@ngx-loading-bar/http-client";
import { provideLoadingBarRouter } from "@ngx-loading-bar/router";
import { NgHcaptchaModule } from "ng-hcaptcha";

if (environment.production) {
    enableProdMode();
}

async function bootstrap() {
    const providers = [
        provideRouter(getAppRoutes(), withExperimentalPlatformNavigation(), withAutoCleanupInjectors(), withViewTransitions(), withComponentInputBinding()),
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([authInterceptor]), withInterceptorsFromDi()),
        provideLoadingBarRouter(),
        provideLoadingBarInterceptor(),
        importProvidersFrom(
            NgHcaptchaModule.forRoot({
                siteKey:
                    !window.location.hostname.includes("localhost") && environment.production
                        ? "98335e9b-d163-4a4a-9a12-80d040a3b7d8"
                        : "10000000-ffff-ffff-ffff-000000000001",
            })
        ),
        provideServiceWorker("ngsw-worker.js", {
            enabled: environment.production,
        }),
        { provide: TitleStrategy, useClass: TemplatePageTitleStrategy },
        provideAppInitializer(() => {
            // Configure MatIconRegistry to use material-symbols as the default font set
            const iconRegistry = inject(MatIconRegistry);
            iconRegistry.setDefaultFontSetClass("material-symbols");
        }),
        provideAppInitializer(() => {
            const initializerFn = initAuthStore();
            return initializerFn();
        }),
    ];
    await bootstrapApplication(AppComponent, {
        providers: [...providers],
    });
}

bootstrap().catch((err) => console.error(err));
