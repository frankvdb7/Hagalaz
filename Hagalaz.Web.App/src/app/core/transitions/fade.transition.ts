import { animate, state, style, transition, trigger } from "@angular/animations";

export const fadeTransition = trigger("fade", [
    state("void", style({ opacity: 0 })),
    state("*", style({ opacity: 1 })),
    transition(":enter", animate("250ms ease-in-out", style({ opacity: 1 }))),
    transition(":leave", animate("250ms ease-in-out", style({ opacity: 0, visibility: "hidden" }))),
]);
