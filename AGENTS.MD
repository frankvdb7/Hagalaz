# Hagalaz Agent Guide

Welcome, agent! This document provides instructions for working with the Hagalaz repository.

## Project Overview

Hagalaz is a modern, full-stack RuneScape Private Server (RSPS) built with a microservice architecture.

-   **Backend:** The backend is built with C# using .NET and ASP.NET. It's orchestrated by .NET Aspire. The main services include:
    -   `Hagalaz.Services.GameWorld`: Manages the game world, characters, and game logic.
    -   `Hagalaz.Services.GameUpdate`: Provides game client updates.
    -   `Hagalaz.Services.Characters`: Manages character data.
    -   `Hagalaz.Services.Authorization`: Handles authentication and authorization.
-   **Frontend:** The frontend is an Angular single-page application (SPA) located in the `Hagalaz.Web.App` directory. It also includes an Electron-based launcher.
-   **Infrastructure:** The project is designed to run on Docker and Kubernetes, using RabbitMQ for messaging, MySQL for data storage, and Redis for caching.

## 1. Environment Setup

### Prerequisites

1.  **.NET SDK:** Ensure the .NET SDK (version specified in `global.json`) is installed.
2.  **.NET Aspire:** Install the .NET Aspire workload.
3.  **Node.js:** Ensure Node.js (version specified in `Hagalaz.Web.App/package.json`) and pnpm are installed.
4.  **Docker:** Docker Desktop must be running.
5.  **RSPS Cache:** This project requires a specific game cache that is **not** included in this repository. You must obtain it separately and place it in a `Cache` directory at the root of the repository. **The application will not run without this cache.**

### Installation

1.  **Backend Dependencies:** The .NET dependencies are managed by NuGet and will be restored automatically.
2.  **Frontend Dependencies:** Navigate to the `Hagalaz.Web.App` directory and run `pnpm install` to install the Node.js dependencies.

    ```bash
    cd Hagalaz.Web.App
    pnpm install
    cd ..
    ```

## 2. Building and Running the Project

The entire application stack is managed by .NET Aspire.

### Running the Application

1.  **Start the Backend and Services:** Run the `Hagalaz.AppHost` project. This will start all the necessary services (including databases and message queues) in Docker containers and launch the backend microservices.

    ```bash
    dotnet run --project Hagalaz.AppHost
    ```

2.  **Start the Frontend:** In a separate terminal, navigate to the `Hagalaz.Web.App` directory and run the `start` script.

    ```bash
    cd Hagalaz.Web.App
    pnpm start
    ```

The .NET Aspire dashboard will be available at the URL provided in the console output from the `AppHost`, allowing you to monitor the services.

## 3. Running Tests

The solution contains a comprehensive suite of tests.

-   **Backend Tests:** To run all backend tests, execute the following command from the root of the repository:

    ```bash
    dotnet test Hagalaz.sln
    ```

-   **Frontend Tests:** To run the frontend unit tests, navigate to the `Hagalaz.Web.App` directory and run the `test` script:

    ```bash
    cd Hagalaz.Web.App
    pnpm test
    ```

Ensure all relevant tests pass before submitting any changes.

## 4. Solution Structure

The solution is organized into several folders:

-   `Hagalaz.AppHost/`: The .NET Aspire application host that orchestrates all services.
-   `Hagalaz.ServiceDefaults/`: Default configurations for the services.
-   `Services/`: Contains the backend microservices (e.g., `Hagalaz.Services.GameWorld`, `Hagalaz.Services.Authorization`).
-   `Libraries/`: Contains shared libraries and abstractions used across the solution (e.g., `Hagalaz.Data`, `Hagalaz.Game.Abstractions`).
-   `Testing/`: Contains the test projects for the services and libraries.
-   `Hagalaz.Web.App/`: The Angular frontend application.

## 5. Coding Conventions

-   Follow the existing coding style and conventions in the C# and TypeScript files.
-   Use the `.editorconfig` file to maintain consistent formatting.
-   For any new feature, consider if it requires changes to multiple services and how they will communicate.
-   Ensure that any public-facing API changes are reflected in the OpenAPI documentation.
