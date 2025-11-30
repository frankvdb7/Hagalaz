# Hagalaz

A modern, full-stack recreation of a classic MMORPG, built with a microservice architecture and designed for scalability, maintainability, and a rich developer experience.

Hagalaz means "hail" in Proto-Germanic and represents natural disruption and transformation. Symbolizing the uncontrollable forces of nature, it signifies unexpected challenges that lead to growth and renewal.

[![License](https://img.shields.io/badge/license-GPLv3-blue)](LICENSE)

## Description

This repository is a comprehensive application built using modern development technologies and best practices. It leverages a powerful stack to deliver a robust and scalable platform.

### Core Technologies

- **Backend**: .NET 10, C#, ASP.NET Core
- **Frontend**: Angular, Electron
- **Orchestration**: .NET Aspire
- **Infrastructure**: Docker, MySQL, RabbitMQ, Redis
- **Communication**: REST APIs, gRPC, WebSockets

### Key Libraries & Standards

- **Messaging**: MassTransit for reliable, asynchronous communication.
- **Authentication**: OpenIddict implementing OAuth2 and OpenID Connect.
- **Resilience**: Polly for transient-fault handling and resilience patterns.
- **Caching**: FusionCache with Redis for high-performance data caching.
- **Observability**: OpenTelemetry for standardized logs, metrics, and traces.
- **API Standards**: OpenAPI for documentation, tested with Scalar.

## Project Structure

The solution is organized into a microservice architecture, with clear separation of concerns between projects.

- **`Hagalaz.AppHost`**: The .NET Aspire orchestration project. This is the entry point for running the application locally. It defines all the services, databases, and other resources, and manages their configuration and lifecycle.

- **`Hagalaz.ApiService`**: The public-facing API gateway. It acts as a reverse proxy (YARP) and the primary entry point for the Angular client, routing requests to the appropriate backend services and handling cross-cutting concerns like authentication.

- **`Services/`**: This directory contains the individual microservices that make up the application's backend logic.

  - **`Hagalaz.Services.GameWorld`**: Manages core gameplay logic, character state, and interactions within the game world.
  - **`Hagalaz.Services.Login`**: Handles the player login and character selection process.
  - **`Hagalaz.Services.Store`**: (Example) Manages in-game shops or other transactional features.
  - _(Other services follow this pattern)_

- **`Libraries/`**: Contains shared libraries and abstractions used across multiple services to reduce code duplication and enforce consistency.

  - **`Hagalaz.Game.Abstractions`**: The foundational project for the game's domain model. It defines the core interfaces (`ICharacter`, `IItem`), enums, and data structures that represent all entities and concepts within the game world.
  - **`Hagalaz.Cache`**: A dedicated library for reading and parsing the game's data cache files.
  - **`Hagalaz.Network.Common`**: Provides common networking utilities, including packet composition and read/write operations.
  - **`Hagalaz.Security`**: Implements security-related functionalities like data encryption and hashing.

- **`Tests/`**: Contains all unit, integration, and end-to-end tests for the solution, ensuring code quality and reliability.

## Prerequisites

Before you begin, ensure you have the following installed:

- **[.NET 10 SDK](https://dotnet.microsoft.com/download)**
- **[.NET Aspire Workload](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/build-your-first-aspire-app#install-the-net-aspire-workload)**
- **[Docker Desktop](https://www.docker.com/products/docker-desktop/)**
- A compatible game client and its corresponding data cache. The cache files should be placed in a `/Cache` directory at the root of the solution.

## Getting Started

### 1. Running the Application

The easiest way to get the entire application running is by using the .NET Aspire AppHost project.

1.  **Clone the repository:**

    ```bash
    git clone https://github.com/your-username/hagalaz.git
    cd hagalaz
    ```

2.  **Run the AppHost:**

    ```bash
    dotnet run --project Hagalaz.AppHost/Hagalaz.AppHost.csproj
    ```

3.  **Launch the Aspire Dashboard:**
    Once the project is running, .NET Aspire will start all the configured services, databases, and containers. You can view the status, logs, and traces of all resources in the Aspire Dashboard, which typically launches automatically in your web browser.

### 2. Configuration

Most of the service discovery and configuration is handled automatically by .NET Aspire. However, service-specific settings can be found and modified in the `appsettings.json` file of each individual service project (e.g., `Hagalaz.Services.GameWorld/appsettings.json`).

### 3. Development Workflow

A typical workflow for adding a new feature might look like this:

1.  **Define Contracts**: Add or update interfaces and models in `Hagalaz.Game.Abstractions`.
2.  **Implement Service Logic**: Implement the new business logic within the relevant microservice in the `Services/` directory.
3.  **Add API Endpoints**: Expose the new functionality via an API endpoint in the service.
4.  **Write Tests**: Add unit and integration tests for the new logic in the corresponding `Tests/` project.
5.  **Consume in Client**: Update the Angular frontend to consume the new endpoint.

## Contributing

Contributions are welcome! Please fork the repository, create a new branch for your feature or fix, and submit a pull request.

## License

This project is licensed under the GNU General Public License v3.0. See the [LICENSE](LICENSE) file for details.

## Contact

- **Author**: Frank
- **GitHub**: [frankvdb7](https://github.com/frankvdb7)
