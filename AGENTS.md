# Hagalaz Agent Guide

This document provides guidance for AI agents working on the Hagalaz codebase.

## Project Overview

Hagalaz is a modern, open-source recreation of a classic MMORPG. It features a microservices architecture for the backend and an Angular/Electron application for the frontend.

- **Backend**: .NET 10 with ASP.NET Core and .NET Aspire for orchestration. The backend is composed of multiple microservices, as can be seen in the `Hagalaz.sln` solution file.
- **Frontend**: Angular with Angular Material and Tailwind CSS. The frontend is located in the `Hagalaz.Web.App` directory and is also an Electron application.
- **Database**: The `README.md` mentions MySQL. It is safe to assume a SQL database is used with Entity Framework.
- **Infrastructure**: The project uses Docker for containerization, RabbitMQ for messaging, and Redis for caching.

## Getting Started

### Backend

The backend services are orchestrated by .NET Aspire. To run the backend:

1.  Navigate to the `Hagalaz.AppHost` directory:
    ```bash
    cd Hagalaz.AppHost
    ```
2.  Run the application:
    ```bash
    dotnet run
    ```

This will start all the backend services as defined in the `Hagalaz.AppHost` project.

### Frontend

The frontend is an Angular application. To run the frontend:

1.  Navigate to the `Hagalaz.Web.App` directory:
    ```bash
    cd Hagalaz.Web.App
    ```
2.  Install the dependencies using pnpm:
    ```bash
    pnpm install
    ```
3.  Start the development server:

    ```bash
    pnpm start
    ```

    This will run the web application, which can be accessed in a browser.

4.  To run the Electron application:
    ```bash
    pnpm run launcher:start
    ```

## Building the Project

### Backend

To build the entire .NET solution, run the following command from the root directory:

```bash
dotnet build Hagalaz.sln
```

### Frontend

To build the Angular application, navigate to the `Hagalaz.Web.App` directory and run:

```bash
pnpm run build
```

For a production build of the launcher, use:

```bash
pnpm run launcher:build
```

## Testing

### Backend

The backend has a suite of unit tests. To run them, execute the following command from the root directory:

```bash
dotnet test Hagalaz.sln
```

The CI pipeline in `.github/workflows/ci.yml` runs these tests on every push and pull request.

### Frontend

The frontend has unit tests that can be run with Vitest. To run them, navigate to the `Hagalaz.Web.App` directory and run:

```bash
pnpm test
```

The CI pipeline in `.github/workflows/ci.yml` also runs these tests.

## Key Files

- `README.md`: General information about the project.
- `Hagalaz.sln`: The main solution file for the .NET projects.
- `global.json`: Specifies the .NET SDK version.
- `Hagalaz.AppHost/Hagalaz.AppHost.csproj`: The entry point for running the backend services with .NET Aspire.
- `Hagalaz.Web.App/package.json`: Defines the dependencies and scripts for the frontend application.
- `.github/workflows/ci.yml`: The CI pipeline definition for GitHub Actions.
