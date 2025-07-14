EFDemo — Distributed Transactions and Outbox Pattern Example in .NET 8
EFDemo is a demo project built with ASP.NET Core 8 and Entity Framework Core, showcasing distributed transactions and the Outbox pattern for microservices architecture. The solution follows Clean Architecture principles to ensure modularity, testability, and scalability. It demonstrates how to reliably integrate messaging and database operations in modern .NET applications.

Prerequisites
Docker (for SQL Server, Redis, RabbitMQ)
.NET 6/7/8 SDK
Outbox Pattern for Reliable Event Publishing
This project demonstrates the Outbox pattern, a proven approach for reliable event publishing in distributed systems. The Outbox pattern ensures that business data changes and event publication are atomic and consistent, even in the face of failures.

Application Steps: Description, Issue, Solution
Step 1: Performance Optimization Demos (EF Core)

Description: Runs a series of EF Core demos showing common performance pitfalls (N+1 queries), and solutions (eager loading, explicit loading, no-tracking, projection, caching).
Issue: Naive data access patterns can cause excessive database queries, slow performance, and high resource usage.
Solution: Demonstrates best practices like eager loading, projections, and caching to optimize data access and application performance.
Step 2: Run Performance Benchmarks (EF Core vs Dapper)

Description: Executes benchmarks comparing EF Core and Dapper for various query scenarios using BenchmarkDotNet.
Issue: Developers may not know which ORM or data access strategy is fastest for their workload.
Solution: Provides empirical performance data to guide technology and query pattern choices.
Step 3: Complex Mappings Demo (TPH/TPT/TPC)

Description: Demonstrates EF Core's inheritance mapping strategies: Table-per-Hierarchy (TPH), Table-per-Type (TPT), and Table-per-Concrete-Type (TPC), including schema and sample data.
Issue: Choosing the wrong mapping strategy can lead to inefficient queries or difficult schema evolution.
Solution: Shows the pros, cons, and schema impact of each mapping, helping you select the right approach for your domain model.
Step 4: Transactions Demo (Outbox Pattern)

Description: Runs a distributed transaction demo using the Outbox pattern: saves business data and an Outbox message in a single transaction, then publishes the event reliably. The Outbox pattern ensures that business data changes and event publication are atomic and consistent, even in the face of failures. The process includes:
Saving both business data and an Outbox message in a single transaction.
Issue: If the app crashes after saving data but before publishing the event, the event could be lost.
Solution: Saving both in one transaction ensures atomicity: either both are saved, or neither.
A background service polling the Outbox table for unsent messages and publishing them to the message broker (e.g., RabbitMQ).
Issue: If the app crashes after the DB commit but before publishing, the event would not be sent.
Solution: The background service will retry until the message is published, guaranteeing delivery.
Marking the Outbox message as processed after successful publishing.
Issue: Without tracking, the same message could be sent multiple times or never marked as sent.
Solution: Marking as processed ensures each event is published exactly once.
A consumer service receiving and processing the published event.
Issue: Consumers may fail or process events multiple times.
Solution: Consumers should be idempotent and handle retries gracefully.
Periodic cleanup of processed Outbox messages older than 30 days.
Issue: The Outbox table could grow indefinitely, impacting performance.
Solution: Regular cleanup keeps the table size manageable and performance high.
Issue: Directly publishing events after DB changes risks data/event inconsistency if the app crashes mid-process.
Solution: The Outbox pattern ensures atomicity and reliable event delivery, even in the face of failures.
Step 5: Migrations & Seeding Demo

Description: Applies EF Core migrations and seeds the database with a small set of demo data (students and teachers with addresses).
Issue: Manual database setup and inconsistent test data can slow development and testing.
Solution: Automates schema migration and data seeding for a consistent, ready-to-use development environment.
Step 0: Exit

Description: Exits the application.
Quick Start
Start all services:
docker-compose up -d
Apply EF Core migrations for your domain:
dotnet ef database update --context AppDbContext
Run the app:
dotnet run
