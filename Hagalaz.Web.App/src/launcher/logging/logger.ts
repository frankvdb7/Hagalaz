import { createLogger, format, type Logger, transports } from "winston";
import { injectable } from "inversify";

@injectable()
export abstract class ILogger {
    abstract error(message: string, error?: unknown): void;

    abstract warn(message: string, error?: unknown): void;
}

@injectable()
export class WinstonLogger implements ILogger {
    private readonly _logger: Logger;

    constructor() {
        this._logger = createLogger({
            level: "debug",
            format: format.json(),
            transports: [
                new transports.Console({
                    format: format.simple(),
                }),
            ],
        });
    }

    error(message: string, error?: unknown): void {
        this._logger.error(message, error);
    }

    warn(message: string, error?: unknown): void {
        this._logger.warn(message, error);
    }
}
