import { Injectable, Signal, signal } from "@angular/core";

@Injectable({
    providedIn: "root",
})
export class BackgroundImageService {
    private readonly defaultBackgroundImageUrl = "assets/images/bg.jpg";
    public readonly backgroundImageUrl = signal<string>(this.defaultBackgroundImageUrl);

    set(url: string) {
        this.backgroundImageUrl.set(url);
    }

    reset() {
        this.backgroundImageUrl.set(this.defaultBackgroundImageUrl);
    }
}
