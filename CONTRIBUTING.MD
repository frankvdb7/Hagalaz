# Contributing to Hagalaz

First off, thank you for considering contributing to Hagalaz! It's people like you that make open source great.

## Where do I go from here?

If you've noticed a bug or have a feature request, [make one](https://github.com/frankvdb7/Hagalaz/issues/new/choose)! It's generally best if you get confirmation of your bug or approval for your feature request this way before starting to code.

### Fork & create a branch

If this is something you think you can fix, then [fork Hagalaz](https://github.com/frankvdb7/Hagalaz/fork) and create a branch with a descriptive name.

A good branch name would be (where issue #38 is the ticket you're working on):

```sh
git checkout -b 38-add-party-hat-drop
```

### Get the code running

The `README.md` has instructions for how to get the project running. A key prerequisite is obtaining the **RSPS Cache** and placing it in a `/Cache` directory at the root of the repository. Without this, the application will not run.

The project is orchestrated using .NET Aspire. You can start all the necessary services by running:

```bash
dotnet run --project Hagalaz.AppHost
```

The frontend application is in the `Hagalaz.Web.App` directory. To run it, you'll need to install dependencies with `pnpm install` and then start the development server with `pnpm start`.

### Make your changes

Make your changes to the codebase. Be sure to follow the existing coding style.

### Test your changes

Ensure that all backend and frontend tests pass before submitting your pull request.

-   **Backend tests:** `dotnet test Hagalaz.sln`
-   **Frontend tests:** `cd Hagalaz.Web.App && pnpm test`

### Commit your changes

Write a clear and concise commit message for your changes.

### Submit a Pull Request

When you're ready, submit a pull request to the `main` branch of the Hagalaz repository.

## Code of Conduct

This project and everyone participating in it is governed by the [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior.
