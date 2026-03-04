import { provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from "@angular/core";
import { provideHttpClient, withFetch } from "@angular/common/http";
import { provideRouter } from "@angular/router";

export default [provideZonelessChangeDetection(), provideBrowserGlobalErrorListeners(), provideHttpClient(withFetch()), provideRouter([])];
