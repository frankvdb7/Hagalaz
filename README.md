# Hagalaz

A modern, full-stack RSPS built with a microservice architecture, designed for scalability and maintainability.

Hagalaz means "hail" in Proto-Germanic and represents natural disruption and transformation. Symbolizing the uncontrollable forces of nature, it signifies unexpected challenges that lead to growth and renewal.

[![License](https://img.shields.io/badge/license-GPLv3-blue)](LICENSE)

## Description

This repository is a comprehensive application built using modern development technologies and best practices. It leverages:

- **ASP.NET Core**
- **.NET Aspire**
- **Docker**
- **Kubernetes**
- **Masstransit**
- **RabbitMQ**
- **OpenIddict**
- **Polly**
- **FluentResults**
- **MySQL**
- **Swagger/OpenAPI**
- **Refit**
- **Redis**
- **FusionCache**
- **OpenTelemetry**
- **Scrutor**
- **YARP**

## Features

- **Scalable Microservices Architecture**: Designed for scalability and fault tolerance.
- **Authentication & Authorization**: Implemented with OpenIddict.
- **Distributed Messaging**: Powered by Masstransit for reliable communication between services.
- **API Documentation**: Interactive documentation with Swagger/OpenAPI.
- **Resilience**: Using Polly for transient-fault handling.
- **Type-safe APIs**: With Refit for clean HTTP client integration.
- **Caching**: Optimized with Redis and FusionCache.
- **Telemetry & Analytics**: Standarized with OpenTelemetry.
- **Containerization**: Fully containerized for portability using Docker and Kubernetes.
- **Dynamic Proxying**: Achieved through YARP.

## Prerequisites

- Valid RSPS Cache (placed in /Cache)
- Valid RSPS Client
- [.NET SDK](https://dotnet.microsoft.com/download)
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Docker](https://www.docker.com/)
- [Kubernetes](https://kubernetes.io/) or a local cluster (e.g., Minikube or Docker Desktop)

## Getting Started

- Run / Debug the Hagalaz.AppHost project to let .NET Aspire start and orchestrate the required services in Docker locally.
- Deploy the Hagalaz.AppHost project with .NET Aspire and Aspirate to a remote Kubernetes cluster or locally (e.g., Minkube or Docker)

## Contributing

Contributions are welcome! Please fork the repository and submit a pull request.

## License

This project is licensed under the GNU General Public License v3.0. See the [LICENSE](LICENSE) file for details.

## Contact

- **Author**: Frank
- **GitHub**: [frankvdb7](https://github.com/frankvdb7)
