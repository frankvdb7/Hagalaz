import { animate, query, style, transition, trigger, stagger, sequence, AnimationMetadata } from "@angular/animations";
import { RouteTransitionService } from "./route.transition.service";

export const ROUTE_TRANSITIONS_ELEMENTS = "route-transition-elements";

const STEPS_ALL: AnimationMetadata[] = [
    query(":leave", style({ position: "absolute", inset: 0 }), {
        optional: true,
    }),
    query(":enter > *", style({ opacity: 0, position: "fixed" }), {
        optional: true,
    }),
    query(":enter ." + ROUTE_TRANSITIONS_ELEMENTS, style({ opacity: 0 }), {
        optional: true,
    }),
    sequence([
        query(
            ":leave > *",
            [
                style({ transform: "translateY(0%)", opacity: 1 }),
                animate("0.2s ease-in-out", style({ transform: "translateY(-3%)", opacity: 0 })),
                style({ position: "absolute" }),
            ],
            { optional: true }
        ),
        query(
            ":enter > *",
            [
                style({
                    transform: "translateY(-3%)",
                    opacity: 0,
                    position: "static",
                }),
                animate("0.5s ease-in-out", style({ transform: "translateY(0%)", opacity: 1 })),
            ],
            { optional: true }
        ),
    ]),
    query(
        ":enter ." + ROUTE_TRANSITIONS_ELEMENTS,
        stagger(100, [style({ transform: "translateY(3%)", opacity: 0 }), animate("0.5s ease-in-out", style({ transform: "translateY(0%)", opacity: 1 }))]),
        { optional: true }
    ),
];
const STEPS_NONE: AnimationMetadata[] = [];
const STEPS_PAGE = [STEPS_ALL[0], STEPS_ALL[2]];
const STEPS_ELEMENTS = [STEPS_ALL[1], STEPS_ALL[3]];

export const routeTransitions = trigger("routeTransitions", [
    transition(isRouteTransitionsAll, STEPS_ALL),
    transition(isRouteTransitionsNone, STEPS_NONE),
    transition(isRouteTransitionsPage, STEPS_PAGE),
    transition(isRouteTransitionsElements, STEPS_ELEMENTS),
]);

export function isRouteTransitionsAll() {
    return RouteTransitionService.isRouteTransitionsType("ALL");
}

export function isRouteTransitionsNone() {
    return RouteTransitionService.isRouteTransitionsType("NONE");
}

export function isRouteTransitionsPage() {
    return RouteTransitionService.isRouteTransitionsType("PAGE");
}

export function isRouteTransitionsElements() {
    return RouteTransitionService.isRouteTransitionsType("ELEMENTS");
}
