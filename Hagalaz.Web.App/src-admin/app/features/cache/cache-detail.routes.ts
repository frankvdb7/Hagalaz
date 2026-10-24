import type { Routes } from "@angular/router";

export const cacheDetailRoutes: Routes = [
    { path: "types/items/:id", loadComponent: () => import("./pages/details/items-detail.page").then((m) => m.ItemsDetailPageComponent) },
    { path: "types/npcs/:id", loadComponent: () => import("./pages/details/npcs-detail.page").then((m) => m.NpcsDetailPageComponent) },
    { path: "types/objects/:id", loadComponent: () => import("./pages/details/objects-detail.page").then((m) => m.ObjectsDetailPageComponent) },
    { path: "types/animations/:id", loadComponent: () => import("./pages/details/animations-detail.page").then((m) => m.AnimationsDetailPageComponent) },
    { path: "types/maps/:id", loadComponent: () => import("./pages/details/maps-detail.page").then((m) => m.MapsDetailPageComponent) },
    { path: "types/sprites/:id", loadComponent: () => import("./pages/details/sprites-detail.page").then((m) => m.SpriteDefinitionsDetailPageComponent) },
    { path: "types/quests/:id", loadComponent: () => import("./pages/details/quests-detail.page").then((m) => m.QuestsDetailPageComponent) },
    { path: "types/graphics/:id", loadComponent: () => import("./pages/details/graphics-detail.page").then((m) => m.GraphicsDetailPageComponent) },
    { path: "types/varp-bits/:id", loadComponent: () => import("./pages/details/varp-bits-detail.page").then((m) => m.VarpBitsDetailPageComponent) },
    {
        path: "types/client-map-definitions/:id",
        loadComponent: () => import("./pages/details/client-map-definitions-detail.page").then((m) => m.ClientMapDefinitionsDetailPageComponent),
    },
    {
        path: "types/config-definitions/:id",
        loadComponent: () => import("./pages/details/config-definitions-detail.page").then((m) => m.ConfigDefinitionsDetailPageComponent),
    },
    { path: "types/cs2/:id", loadComponent: () => import("./pages/details/cs2-detail.page").then((m) => m.Cs2DetailPageComponent) },
    { path: "types/cs2-int/:id", loadComponent: () => import("./pages/details/cs2-int-detail.page").then((m) => m.Cs2IntDetailPageComponent) },
];
