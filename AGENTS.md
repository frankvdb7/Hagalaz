# AGENTS.md

This file provides instructions for AI coding agents to work effectively with the Hagalaz project.

## Project Overview
- **Hagalaz** is a modern, full-stack RuneScape Private Server (RSPS) built with a microservice architecture.
- It is designed for scalability and maintainability, leveraging a wide range of modern technologies.
- The backend is built with **ASP.NET** and **.NET Aspire** on **.NET 9**.
- The frontend is an **Angular** Single Page Application.
- The infrastructure is containerized using **Docker** and orchestrated with **Kubernetes**.
- It uses **RabbitMQ** for distributed messaging, **MySQL** for the database, and **Redis** for caching.

## Getting Started
- The project uses the **.NET SDK version 9.0.5**, as specified in `global.json`.
- To get started locally, you can run or debug the `Hagalaz.AppHost` project. This will use .NET Aspire to start and orchestrate all the required services in Docker.

## Build and Test
The CI/CD pipeline is defined in `bitbucket-pipelines.yml`. The main commands are:
- **Restore dependencies:** `dotnet restore`
- **Build the project:** `dotnet build --no-restore`
- **Run tests:** `dotnet test --no-build --logger:trx`

When working on the project, please ensure that all tests pass before submitting a pull request.

## Code Style
- Follow the standard C# and .NET coding conventions.
- Use 4 spaces for indentation.
- Private fields should be prefixed with an underscore (`_`).
- Always include braces for `if`, `for`, `while` etc. statements, even for single lines.
- Pay attention to the existing code style in the files you are editing and try to maintain consistency.

## Project Structure
- The solution is organized into multiple projects, each with a specific responsibility (microservices).
- `Hagalaz.AppHost`: The .NET Aspire application host for orchestrating the services.
- `Hagalaz.Web.App`: The main web application service.
- Other `Hagalaz.*` projects are individual services or shared libraries.

## Pull Requests
- PR titles should be descriptive and follow a conventional commit format if possible (e.g., `feat:`, `fix:`, `docs:`).
- The PR description should clearly explain the changes made and the problem they solve.
- Ensure your changes build successfully and all tests pass before requesting a review.
