import { Component, OnInit, ChangeDetectionStrategy, OnDestroy, TemplateRef, ViewContainerRef, AfterViewInit, inject, viewChild } from "@angular/core";
import { Overlay, OverlayRef } from "@angular/cdk/overlay";
import { TemplatePortal } from "@angular/cdk/portal";
import { LoadingComponent } from "../loading/loading.component";

@Component({
    selector: "app-loading-overlay",
    templateUrl: "./loading-overlay.component.html",
    styleUrls: ["./loading-overlay.component.scss"],
    imports: [LoadingComponent],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoadingOverlayComponent implements AfterViewInit, OnDestroy {
    private overlay = inject(Overlay);
    private viewRef = inject(ViewContainerRef);

    private overlayRef!: OverlayRef;
    readonly loadingRef = viewChild.required<TemplateRef<any>>("loading");

    ngAfterViewInit(): void {
        const position = this.overlay.position().global().centerHorizontally().centerVertically();
        this.overlayRef = this.overlay.create({
            hasBackdrop: true,
            positionStrategy: position,
        });
        this.overlayRef.attach(new TemplatePortal(this.loadingRef(), this.viewRef));
    }

    ngOnDestroy(): void {
        this.overlayRef?.dispose();
    }
}
