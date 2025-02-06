import { createLogger, format, Logger, transports } from "winston";
import { injectable } from "inversify";

@injectable()
export abstract class ILogger {
    abstract debug(message: string, error?: unknown): void;

    abstract error(message: string, error?: unknown): void;

    abstract info(message: string, error?: unknown): void;

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

    debug(message: string, error?: unknown): void {
        this._logger.debug(message, error);
    }

    error(message: string, error?: unknown): void {
        this._logger.error(message, error);
    }

    info(message: string, error?: unknown): void {
        this._logger.info(message, error);
    }

    warn(message: string, error?: unknown): void {
        this._logger.warn(message, error);
    }
}
