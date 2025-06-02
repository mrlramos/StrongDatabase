# StrongDatabase

Modern educational project demonstrating distributed architecture with .NET 8, PostgreSQL, and Docker Compose, featuring real replication, failover, and load balancing.

---

## Technologies Used
- **.NET 8 (ASP.NET Core Web API)**
- **PostgreSQL 16** (Primary, Standby, Replica1, Replica2)
- **Docker Compose**

---

## Project Structure
```
StrongDatabase/
│
├── StrongDatabase.Api/         # .NET application source code
├── docker/                     # Database configurations and scripts
│   ├── primary/                # Primary database config
│   ├── standby/                # Standby config (synchronous)
│   ├── replica1/               # Replica 1 config (asynchronous)
│   └── replica2/               # Replica 2 config (asynchronous)
├── scripts/                    # SQL scripts for database creation and data
├── docker-compose.yml          # Container orchestration
└── README.md                   # Documentation
```

---

## Project Objectives
- Demonstrate **distributed database architecture** with real replication (synchronous and asynchronous)
- **Automatic and secure failover** (zero data loss)
- **Distributed reading** across replicas
- **Intelligent load balancing** of connections in the application
- **Health checks** and monitoring
- **Practical example** of .NET + PostgreSQL + Docker Compose integration

---

## Architecture and Flow

### 1. **Containers and Functions**
- **primary-db**: Main database (write and emergency read)
- **standby-db**: Synchronous standby (failover, never loses data)
- **replica1-db/replica2-db**: Asynchronous replicas (distributed reading)
- **strongdatabase-api**: .NET 8 API, performs automatic load balancing and failover

### 2. **Replication and Failover**
- The **primary** replicates via WAL to standby (synchronous) and replicas (asynchronous)
- The **standby** only confirms write when it receives the data (guarantees zero loss)
- The **replicas** receive changes asynchronously (may lag a few seconds)
- If primary fails, application redirects writes to standby
- If all replicas and primary fail, reads go to standby

#### 🔎 How Synchronous Synchronization Works in PostgreSQL

Synchronous synchronization in databases like PostgreSQL ensures that data written to the primary database is replicated to the standby database before confirming the transaction to the client. This guarantees zero data loss in case of primary failure.

**How it works:**
- **Write to primary:** When a transaction (INSERT, UPDATE, DELETE) is executed on the primary database, data is recorded in the WAL (Write-Ahead Log), a transaction log.
- **Send to standby:** The WAL is sent to the standby database in real-time via streaming replication.
- **Synchronous confirmation:** The primary waits for confirmation from the standby that WAL data has been received and applied (or at least written to disk, depending on configuration).
- **Commit on primary:** Only after standby confirmation does the primary confirm the transaction to the client.
- **Safe failover:** If the primary fails, the standby already has all confirmed data, allowing it to take over as new primary without loss.

**Behind-the-scenes operations:**
- **WAL Streaming:** Primary sends WAL records to standby via TCP connection (via wal_sender on primary and wal_receiver on standby).
- **Synchronous Commit:** Configured with `synchronous_commit = on` and `synchronous_standby_names` in primary's postgresql.conf, specifying the standby.
- **Handshaking:** Standby confirms WAL reception/application, and primary waits for this response before proceeding.
- **Latency:** Since primary waits for confirmation, there's a small increase in transaction latency, but this guarantees consistency.

**Typical configuration (PostgreSQL):**
```conf
# postgresql.conf (primary)
wal_level = replica
synchronous_commit = on
synchronous_standby_names = 'standby-db'
max_wal_senders = 10
```

**Trade-offs:**
- **Advantage:** Zero data loss, ideal for critical systems.
- **Disadvantage:** Higher latency, as primary waits for standby.

> **Summary:**
> Synchronous synchronization uses WAL to replicate data in real-time, waiting for standby confirmation before commit, guaranteeing total consistency.

#### 🔎 How Asynchronous Replication Works in PostgreSQL

Asynchronous replication in PostgreSQL allows replicas (read replicas) to receive updates from the primary database without blocking transactions, optimizing distributed reads but with possible data lag.

**How it works:**
- **Write to primary:** Transactions (INSERT, UPDATE, DELETE) are written to the primary database's WAL (Write-Ahead Log).
- **Send to replica:** WAL is sent to replicas via streaming replication, but without waiting for confirmation.
- **Apply on replica:** Replicas apply WAL records independently, which may cause a small delay (eventual consistency).
- **Reads on replicas:** Replicas (hot standby) serve read queries, relieving the primary and scaling read performance.

**Behind-the-scenes operations:**
- **WAL Streaming:** Primary sends WAL to replicas via wal_sender (primary) and wal_receiver (replica).
- **Asynchronous:** Primary confirms transaction to client without waiting for replicas, reducing latency.
- **Hot Standby:** Replicas can process read queries while applying WAL, configured with hot_standby = on.
- **Lag:** Depending on load or network, replicas may be a few seconds behind primary.

**Typical configuration (PostgreSQL):**
```conf
# postgresql.conf (primary)
wal_level = replica
max_wal_senders = 10
hot_standby = on  # (on replicas)
```

**Trade-offs:**
- **Advantage:** Lower write latency, high read scalability.
- **Disadvantage:** Replicas may have slightly outdated data (milliseconds to seconds lag).

> **Summary:**
> Asynchronous replication uses WAL to send data to replicas without blocking the primary, ideal for scaling reads but with eventual consistency.

### 3. **Intelligent Load Balancing (DbContextRouter)**
- **Write:** Always tries primary, if it fails, uses standby
- **Read:** Distributes among replicas, if all fail tries primary, if it fails, standby
- **Informative logs** show each fallback and decision

### 4. **Health Checks**
- `/health` endpoint monitors API and database connections
- Can be used by external orchestrators or load balancers

---

## Data Model

### Tables
- **Customer**: `id`, `name`, `email`
- **Product**: `id`, `name`, `price`
- **Order**: `id`, `customer_id`, `product_id`, `quantity`, `order_date`

### Sample Data
```sql
INSERT INTO cliente (nome, email) VALUES
  ('John Silva', 'john@email.com'),
  ('Mary Souza', 'mary@email.com');
INSERT INTO produto (nome, preco) VALUES
  ('Notebook', 3500.00),
  ('Mouse', 80.00);
INSERT INTO compra (cliente_id, produto_id, quantidade) VALUES
  (1, 1, 1),
  (2, 2, 2);
```

---

## Available Endpoints

### Customers (`/api/customer`)
- `GET /api/customer` — List all customers
- `POST /api/customer` — Create new customer

### Products (`/api/product`)
- `GET /api/product` — List all products
- `POST /api/product` — Create new product

### Orders (`/api/order`)
- `GET /api/order` — List all orders (includes customer and product)
- `POST /api/order` — Create new order

### Health Check and Monitoring
- `GET /health` — Detailed health check with status of all database servers
- `GET /api/health` — Health check via controller with organized information
- `GET /api/health/simple` — Quick API status verification
- `GET /api/health/version` — Detailed version and environment information

#### Health Check Response Example (`/health`)
```json
{
  "status": "healthy",
  "totalDuration": 45.23,
  "results": {
    "database_health_check": {
      "status": "healthy",
      "description": "All services are working correctly",
      "duration": "44.15ms",
      "data": {
        "api": {
          "status": "healthy",
          "version": "1.0.0",
          "environment": "Development",
          "uptime": "00.02:15:30",
          "startTime": "2024-01-15T10:30:00Z",
          "timestamp": "2024-01-15T12:45:30Z"
        },
        "primary": {
          "status": "healthy",
          "responseTimeMs": 12,
          "databaseName": "strongdatabase_primary",
          "user": "primary_user",
          "serverAddress": "172.18.0.2",
          "serverPort": 5432,
          "postgresqlVersion": "16.1",
          "lastCheck": "2024-01-15T12:45:30Z"
        },
        "standby": {
          "status": "healthy",
          "responseTimeMs": 15,
          "databaseName": "strongdatabase_primary",
          "user": "primary_user",
          "serverAddress": "172.18.0.3",
          "serverPort": 5432,
          "postgresqlVersion": "16.1",
          "lastCheck": "2024-01-15T12:45:30Z"
        },
        "replica1": {
          "status": "healthy",
          "responseTimeMs": 8,
          "databaseName": "strongdatabase_primary",
          "user": "primary_user",
          "serverAddress": "172.18.0.4",
          "serverPort": 5432,
          "postgresqlVersion": "16.1",
          "lastCheck": "2024-01-15T12:45:30Z"
        },
        "replica2": {
          "status": "healthy",
          "responseTimeMs": 10,
          "databaseName": "strongdatabase_primary",
          "user": "primary_user",
          "serverAddress": "172.18.0.5",
          "serverPort": 5432,
          "postgresqlVersion": "16.1",
          "lastCheck": "2024-01-15T12:45:30Z"
        },
        "totalCheckDurationMs": 45
      }
    }
  }
}
```

---

## How to Run the Project

1. **Start the containers:**
   ```sh
   docker-compose up --build -d
   ```
2. **Access the API:**
   - [http://localhost:5000/swagger](http://localhost:5000/swagger) (interactive interface)
   - Or use REST endpoints directly

---

## Technical Details and Scripts

### Primary Configuration (`docker/primary/postgresql.conf`)
```conf
listen_addresses = '*'
wal_level = replica
max_wal_senders = 10
wal_keep_size = 64
archive_mode = on
archive_command = 'cd .'
hot_standby = on
synchronous_standby_names = 'standby-db'
```

### Synchronous Standby (`docker/standby/standby-entrypoint.sh`)
- Clones data from primary on startup
- Connects as synchronous standby
- Only confirms write when it receives the data

### Asynchronous Replicas (`docker/replica1/replica-entrypoint.sh`)
- Clone data from primary on startup
- Operate as asynchronous hot standby

### Docker Compose Orchestration
- Each database exposes a different port (`5433`, `5434`, `5435`, `5436`)
- API exposed on `5000`
- Volumes mount custom scripts and configs
- Dependencies ensure initialization order

---

## Load Balancing and Failover Flow (Educational)

1. **Read**
   - API tries to read from replicas (round-robin)
   - If all fail, tries primary
   - If primary fails, tries standby
2. **Write**
   - API always tries primary
   - If primary fails, uses standby
3. **Failover**
   - If primary goes down, standby takes over without data loss
   - Replicas may lag a few seconds (eventual consistency)

---

## Test Examples

### Test Health Check
```sh
# Detailed health check (main endpoint)
curl http://localhost:5000/health

# Health check via controller
curl http://localhost:5000/api/health

# Quick API verification
curl http://localhost:5000/api/health/simple

# Version information
curl http://localhost:5000/api/health/version
```

### List Customers
```sh
curl http://localhost:5000/api/customer
```

### Create Customer
```sh
curl -X POST http://localhost:5000/api/customer -H "Content-Type: application/json" -d '{"name":"New Customer","email":"new@email.com"}'
```

### Simulate Failover
1. Stop the primary:
   ```sh
   docker stop primary-db
   ```
2. Test health check to see server status:
   ```sh
   curl http://localhost:5000/health
   ```
3. Make a write (POST): API will automatically redirect to standby.
4. Logs will show the fallback.

---

## Educational Notes
- **Synchronous standby** guarantees zero data loss
- **Read replicas** increase read performance
- **Load balancing and failover** are automatic and transparent to the user
- **Production-ready architecture** (with security and monitoring adaptations)

---

## Credits and References
- Educational project inspired by distributed architecture best practices
- Official documentation: [PostgreSQL Streaming Replication](https://www.postgresql.org/docs/current/warm-standby.html)
- [ASP.NET Core Docs](https://learn.microsoft.com/aspnet/core)

---

## How to Clean the Project

To keep the project clean and without unnecessary files, remove the following folders whenever you want:

- `StrongDatabase.Api/bin/`
- `StrongDatabase.Api/obj/`
- `.vs/` (Visual Studio cache, may be in use — close Visual Studio to delete everything)

These folders are automatically generated during build and can be deleted without risk. The command for Windows PowerShell is:

```powershell
Remove-Item -Recurse -Force .\StrongDatabase.Api\bin
Remove-Item -Recurse -Force .\StrongDatabase.Api\obj
Remove-Item -Recurse -Force .\.vs
```

On Linux/Mac:
```bash
rm -rf StrongDatabase.Api/bin StrongDatabase.Api/obj .vs
```

> **Tip:** Before pushing to Git, always clean the project to avoid unnecessary files in the repository!

These practices help keep the repository lean and organized.

---

## Starting the Environment from Scratch (after cleaning everything in Docker)

Whenever you clean all containers, volumes, and images in Docker Desktop, follow this flow to ensure replication works:

1. Start normally:
   ```sh
   docker-compose up --build -d
   ```
   (Wait for all containers to start. Replicas and standby may stop on first attempt, this is expected.)

2. Run the post-up script to adjust replication:
   - **On Windows:**
     ```sh
     scripts\pos-up-windows.bat
     ```
   - **On Linux/Mac:**
     ```sh
     bash scripts/pos-up-linux.sh
     ```

These scripts will:
- Copy the custom `pg_hba.conf` into primary-db
- Restart primary-db
- Restart replicas and standby to ensure replication

Done! The environment will be normalized and functional.

---

**Questions, suggestions, or want to expand? Feel free to contribute!** 