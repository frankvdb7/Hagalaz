import { DestroyRef, DOCUMENT, inject, Service } from "@angular/core";
import { MatDialog } from "@angular/material/dialog";
import { MatSnackBar } from "@angular/material/snack-bar";
import type { CanActivateChildFn, CanDeactivateFn } from "@angular/router";
import { firstValueFrom } from "rxjs";
import { CacheDiscardDialogComponent } from "./pages/cache-discard-dialog.component";

export type CachePendingChangeEditor = { dirty: () => boolean; saving: () => boolean };

/** Reads component-owned drafts; never owns a second copy of their values. */
@Service()
export class CachePendingChangesService {
    private readonly dialog = inject(MatDialog);
    private readonly snackBar = inject(MatSnackBar);
    private editor: CachePendingChangeEditor | null = null;

    constructor() {
        const window = inject(DOCUMENT).defaultView;
        const warn = (event: BeforeUnloadEvent) => {
            if (this.editor?.dirty() || this.editor?.saving()) event.preventDefault();
        };
        window?.addEventListener("beforeunload", warn);
        inject(DestroyRef).onDestroy(() => window?.removeEventListener("beforeunload", warn));
    }

    register(editor: CachePendingChangeEditor, destroyRef: DestroyRef): void {
        this.editor = editor;
        destroyRef.onDestroy(() => {
            if (this.editor === editor) this.editor = null;
        });
    }

    async canLeave(): Promise<boolean> {
        if (this.editor?.saving()) {
            this.snackBar.open("Wait for the cache update to finish before leaving.", "Dismiss");
            return false;
        }
        if (!this.editor?.dirty()) return true;
        const dialog =
            this.dialog.openDialogs.find((dialog) => dialog.componentInstance instanceof CacheDiscardDialogComponent) ??
            this.dialog.open(CacheDiscardDialogComponent, { width: "32rem", maxWidth: "calc(100vw - 2rem)" });
        return (await firstValueFrom(dialog.afterClosed())) === true;
    }
}

// Child activation also runs for reused detail components with a different :id.
export const cacheCanActivateChild: CanActivateChildFn = () => inject(CachePendingChangesService).canLeave();
export const cacheCanDeactivate: CanDeactivateFn<unknown> = () => inject(CachePendingChangesService).canLeave();
