# .NET Observability Demo

**[Official example of dotnet observability from learn.microsoft.com](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-prgrja-example)**

**AI-powered observability showcase for .NET applications**

This project demonstrates modern observability patterns in .NET applications, featuring:
- Distributed tracing with OpenTelemetry
- Metrics
- Example API endpoints for event generation

> ⚠️ **Note:** This codebase was primarily generated using GPT and serves as a reference implementation for testing observability event capture and monitoring workflows.

---

## Technologies Used for Observability

This project leverages a variety of modern technologies to demonstrate observability in a .NET application. Below is the list of key components and their roles:

- **Entity Framework Core with PostgreSQL**
    - Used for database interactions and monitoring database query performance.
    - Observability focus: Tracking slow queries, connection pooling metrics, and transaction tracing.

- **StackExchange.Redis**
    - Provides high-performance Redis caching capabilities.
    - Observability focus: Monitoring cache hit/miss ratios, latency, and connection health.

- **MassTransit**
    - A distributed messaging library for .NET, used for building event-driven architectures.
    - Observability focus: Tracing message flows, tracking consumer processing times, and monitoring queue backlogs.

- **HttpClient**
    - Used for making HTTP requests to external services or APIs.
    - Observability focus: Tracing outgoing HTTP requests, capturing response times, and logging errors.

These technologies are integrated with observability tools such as OpenTelemetry, Prometheus, and Jaeger to provide comprehensive insights into the application's behavior and performance.

## Features

✅ Automatic instrumentation for HTTP requests  
✅ Manual tracing examples with custom attributes  
✅ Prometheus metrics endpoint (/metrics)  
✅ Health checks integration (/health)  
✅ Example background service with periodic metrics  
✅ Docker compose setup for local observability stack (Jaeger, Prometheus, Grafana)

---

## Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- Docker Desktop (for local observability stack)

## Observability Tools
**Jaeger UI: http://localhost:16686 (distributed tracing)  
Prometheus: http://localhost:9090 (metrics)     
Grafana: http://localhost:3000 (pre-built dashboard)**

## Contributing
This project is maintained by AI and humans. While contributions are welcome, please note:
1. The code is intentionally simplified for demonstration purposes
2. Security hardening is not production-ready

### For production use, consider:
- Adding authentication to metrics endpoints
- Implementing proper error handling
- Configuring sampling for high-volume traffic
- Securing observability endpoints