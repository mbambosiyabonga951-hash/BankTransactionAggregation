# 🏦 Bank Transaction Aggregation

**BankTransactionAggregation** is a distributed, event-driven .NET 8 solution for aggregating financial transactions from multiple banking sources into a centralized data store.  
It demonstrates a **Clean Architecture**, **Kafka-based messaging**, **PostgreSQL persistence**, **NLog logging**, and **health monitoring** across microservices.

---

## 🚀 Overview

This system simulates multiple banking APIs (e.g., BankA, BankB) producing transaction events.  
A background **Aggregator Worker** consumes these events from **Kafka**, processes them, and stores normalized data into **PostgreSQL** for analytics and reporting.  
The **Aggregator API** provides endpoints to query and visualize aggregated transactions.

---

## 🧱 Architecture

BankTransactionAggregation/
│
├── Aggregator.Api/ → ASP.NET Core 8 Web API (Query endpoints + Health checks)
├── Aggregator.Worker/ → Background service consuming Kafka topics and persisting data
├── BankA.Api/ → Simulated Bank A transaction publisher
├── BankB.Api/ → Simulated Bank B transaction publisher
│
├── Shared/ → Shared DTOs, messages, constants
├── docker-compose.yml → Local container orchestration (Kafka, Zookeeper, PostgreSQL, UI)
├── nlog.config → Centralized logging configuration
└── BankTransactionAggregation.sln


### 🧩 Components
| Service | Description |
|----------|-------------|
| **Aggregator.Api** | Provides REST endpoints for querying aggregated transactions, health checks, and metrics. |
| **Aggregator.Worker** | Kafka consumer service that processes and stores bank transaction messages. |
| **BankA.Api** | Mock banking service publishing transactions to Kafka. |
| **BankB.Api** | Mock banking service publishing transactions to Kafka. |
| **PostgreSQL** | Centralized database for normalized and aggregated data. |
| **Kafka + Zookeeper** | Messaging backbone for inter-service communication. |
| **Kafka UI** | Web-based UI for monitoring Kafka topics and messages. |

---

## 🏗️ Technologies Used

- **.NET 8 / ASP.NET Core**
- **Entity Framework Core / Dapper (if configured)**
- **PostgreSQL (Npgsql)**
- **Kafka / Confluent.Kafka**
- **Docker Compose**
- **NLog** for structured logging
- **Health Checks** (`/health`)
- **Clean Architecture** (Domain → Application → Infrastructure → Presentation)

---

## ⚙️ Local Setup

### 1️⃣ Prerequisites

Ensure you have installed:

- [Docker Desktop](https://www.docker.com/)
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) or VS Code
- [Postman](https://www.postman.com/) or curl (for testing APIs)

---

### 2️⃣ Run via Docker Compose

```bash
docker-compose up --build
---
###This spins up:

PostgreSQL (localhost:5432)

Kafka broker (localhost:9092)

Kafka UI (http://localhost:8080)

Aggregator API (http://localhost:5000)

Aggregator Worker

BankA API (http://localhost:5001)

BankB API (http://localhost:5002)