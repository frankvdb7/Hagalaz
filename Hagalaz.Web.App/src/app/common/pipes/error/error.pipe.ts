import { Pipe, PipeTransform } from "@angular/core";
import { HttpErrorResponse } from "@angular/common/http";

@Pipe({
    name: "error",
    standalone: true,
    pure: true,
})
export class ErrorPipe implements PipeTransform {
    transform<T extends null | undefined | unknown | object>(value: T): string | T {
        if (typeof value === "object" && value !== null) {
            if (value instanceof HttpErrorResponse) {
                if (value.status === 0) {
                    return "Unable to connect to the server";
                }
            }
            if ("message" in value) {
                return value.message as string;
            }
        }
        return value;
    }
}
