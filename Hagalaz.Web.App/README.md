# AngularSpa

This project was generated with [Angular CLI](https://github.com/angular/angular-cli) version 1.7.0.

## Development server

Run `ng serve` for a dev server. Navigate to `http://localhost:4200/`. The app will automatically reload if you change any of the source files.

## Code scaffolding

Run `ng generate component component-name` to generate a new component. You can also use `ng generate directive|pipe|service|class|guard|interface|enum|module`.

## Build

Run `ng build` to build the project. The build artifacts will be stored in the `dist/` directory. Use the `-prod` flag for a production build.

## Running unit tests

Run `ng test` to execute the unit tests via [Karma](https://karma-runner.github.io).

## Further help

To get more help on the Angular CLI use `ng help` or go check out the [Angular CLI README](https://github.com/angular/angular-cli/blob/master/README.md).

## Publishing Cache Studio map edits

Use **Save** in the map editor to review changes by region and archive. The review lists the old and new values of changed tiles and placements. **Publish changes** writes to the static cache. It leaves live spawns and the running world untouched.

For a protected landscape, open **Landscape XTEA keys** in the inspector. Enter the four signed integers for that region and select **Load landscape**. The editor reuses the entered keys to read that region while the editor is open. Publishing requires the landscape to load successfully with those keys.

Terrain and landscape requests commit independently. If one fails, the editor identifies the confirmed, failed, and unattempted requests. The next publish leaves confirmed writes out and keeps the original versions for pending edits. If a confirmed write cannot be read back into the editor, use **Retry cache reload** to retry that read. If the publish reports a version conflict or an unverified write, check the current cache state before reapplying the affected edits.
