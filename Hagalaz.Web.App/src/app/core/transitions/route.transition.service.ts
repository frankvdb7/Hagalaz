import { Injectable } from "@angular/core";

@Injectable({
  providedIn: "root"
})
export class RouteTransitionService {
  constructor() {}

  private static routeTransitionType: RouteTransitionType = "NONE";

  static isRouteTransitionsType(type: RouteTransitionType) {
    return RouteTransitionService.routeTransitionType === type;
  }

  updateRouteTransitionsType(
    pageAnimations: boolean,
    elementsAnimations: boolean
  ) {
    RouteTransitionService.routeTransitionType =
      pageAnimations && elementsAnimations
        ? "ALL"
        : pageAnimations
          ? "PAGE"
          : elementsAnimations
            ? "ELEMENTS"
            : "NONE";
  }
}

export type RouteTransitionType = "ALL" | "PAGE" | "ELEMENTS" | "NONE";
