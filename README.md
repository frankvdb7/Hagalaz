# Hagalaz

A modern, full-stack RSPS built with a microservice architecture, designed for scalability and maintainability.

Hagalaz means "hail" in Proto-Germanic and represents natural disruption and transformation. Symbolizing the uncontrollable forces of nature, it signifies unexpected challenges that lead to growth and renewal.

[![License](https://img.shields.io/badge/license-GPLv3-blue)](LICENSE)

## Description

This repository is a comprehensive application built using modern development technologies and best practices. It leverages:

### Frameworks
- **ASP .NET**
- **.NET Aspire**
- **Angular**

### Infrastructure
- **Docker**
- **Kubernetes**
- **RabbitMQ**
- **MySQL**
- **Redis**
- **Grafana**
- **Prometheus**
- **YARP**

### Libraries
- **Masstransit**
- **OpenIddict**
- **Scalar**
- **Scrutor**
- **Polly**
- **Refit**
- **FusionCache**
- **FluentResults**

### Standards
- **OpenTelemetry**
- **OpenAPI**
- **OAuth2**

## Features

- **Scalable Microservices Architecture**: Designed for scalability and fault tolerance.
- **Authentication & Authorization**: Implemented with OpenIddict/OAuth2.
- **Distributed Messaging**: Powered by RabbitMQ/Masstransit for reliable communication between services.
- **API Documentation**: Interactive documentation with Scalar/OpenAPI.
- **Resilience**: Using Polly for transient-fault handling.
- **Type-safe APIs**: With Refit for clean HTTP client integration.
- **Caching**: Optimized with Redis/FusionCache.
- **Telemetry & Analytics**: Standardized with OpenTelemetry. Exported to Prometheus. Visualized in Grafana.
- **Containerization**: Fully containerized for portability using Docker and Kubernetes.
- **Dynamic Proxying**: Achieved through YARP.
- **Frontend**: Angular SPA with Material Design.

## Prerequisites

- RSPS Cache (placed in /Cache)
- RSPS Client
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
