import { Routes } from "@angular/router";

export const mainRoutes: Routes = [
    {
        path: "",
        redirectTo: "news",
        pathMatch: "full",
    },
    {
        path: "news",
        title: "News",
        loadChildren: () => import("@app/main/news/news-routing.module").then((m) => m.NewsRoutingModule),
    },
    {
        path: "highscores",
        title: "Highscores",
        loadChildren: () => import("@app/main/highscores/highscores-routing.module").then((m) => m.HighscoresRoutingModule),
    },
    {
        path: "play",
        title: "Play",
        loadChildren: () => import("@app/main/play/play-routing.module").then((m) => m.PlayRoutingModule),
    },
    {
        path: "shop",
        title: "Shop",
        loadComponent: () => import("@app/main/shop/shop.component").then((c) => c.ShopComponent),
    },
    {
        path: "login",
        title: "Login",
        loadChildren: () => import("@app/main/login/login-routing.module").then((m) => m.LoginRoutingModule),
    },
    {
        path: "profile",
        title: "Profile",
        loadChildren: () => import("@app/main/profile/profile-routing.module").then((m) => m.ProfileRoutingModule),
    },
    {
        path: "register",
        title: "Register",
        loadChildren: () => import("@app/main/register/register-routing.module").then((m) => m.RegisterRoutingModule),
    },
    {
        path: "privacy",
        title: "Privacy",
        loadComponent: () => import("@app/main/privacy/privacy-policy-page/privacy-policy-page.component").then((m) => m.PrivacyPolicyPageComponent),
    },
    {
        path: "terms",
        title: "Terms of Service",
        loadComponent: () => import("@app/main/terms/terms-of-service-page/terms-of-service-page.component").then((m) => m.TermsOfServicePageComponent),
    },
    {
        path: "",
        outlet: "header",
        loadChildren: () => import("@app/main/header/header-routing").then((m) => m.routes),
    },
    {
        path: "",
        outlet: "footer",
        loadChildren: () => import("@app/main/footer/footer-routing").then((m) => m.routes),
    },
];

export const routes: Routes = [
    {
        path: "",
        loadComponent: () => import("./main.component").then((c) => c.MainComponent),
        children: mainRoutes,
    },
];
