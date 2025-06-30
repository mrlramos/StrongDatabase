# StrongDatabase 🚀

Modern educational project demonstrating distributed PostgreSQL architecture with .NET 8, featuring real streaming replication, failover, and intelligent load balancing.

## 🎯 **QUICK START (Zero to Running in 2 Commands!)**

### ⚡ **SIMPLE METHOD THAT WORKS** ⚡

#### 🐧 **For Linux/macOS:**
```bash
# 1️⃣ Deploy entire infrastructure
docker-compose up --build -d

# 2️⃣ Setup database replication
./deployment/setup.sh
```

#### 🪟 **For Windows:**
```cmd
# 1️⃣ Deploy entire infrastructure  
docker-compose up --build -d

# 2️⃣ Setup database replication
.\deployment\setup.bat
```

### 🎉 **IN ~3-5 MINUTES YOU'LL HAVE:**
- ✅ **4 PostgreSQL databases** (Primary, Standby, Replica1, Replica2) with real streaming replication
- ✅ **.NET 8 API** with intelligent routing and automatic load balancing
- ✅ **ELK Stack complete** (Elasticsearch, Logstash, Kibana, Filebeat)
- ✅ **Centralized logs** with automatic dashboards
- ✅ **Detailed health checks** for entire infrastructure
- ✅ **Sample data** loaded and ready for testing

### 🌐 **AVAILABLE URLS AFTER DEPLOYMENT:**
| Service | URL | Description |
|---------|-----|-----------|
| 🌐 **Main API** | http://localhost:5000 | REST API with all endpoints |
| 📊 **Swagger/OpenAPI** | http://localhost:5000/swagger | Interactive API documentation |
| 💚 **Health Check** | http://localhost:5000/health | Detailed status of entire infrastructure |
| 📈 **Kibana** | http://localhost:5601 | Dashboards and log analysis |
| 🔍 **Elasticsearch** | http://localhost:9200 | Search engine and log storage |

### 🧪 **QUICK TESTS:**
```bash
# Test API working
curl http://localhost:5000/api/customer

# Check health of all services  
curl http://localhost:5000/health

# Create a new customer
curl -X POST http://localhost:5000/api/customer \
  -H "Content-Type: application/json" \
  -d '{"name":"Test","email":"test@example.com"}'
```

### 🔧 **QUICK TROUBLESHOOTING:**

#### ❌ **If something doesn't work:**
```bash
# 1. Check container status
docker-compose ps

# 2. View logs of specific service
docker logs strongdatabase-api
docker logs primary-db
docker logs logstash

# 3. Restart everything from scratch
docker-compose down -v
docker-compose up --build -d
.\deployment\setup.bat  # or ./deployment/setup.sh
```

#### ⚠️ **Common Issues:**
- **API doesn't start**: Wait ~30 seconds after `docker-compose up`
- **Replicas don't work**: Run the setup script after containers are up
- **Logstash fails**: Normal on first run, it restarts automatically
- **Elasticsearch takes time**: Can take up to 1 minute to become "healthy"

---

## 🏗️ **Architecture Overview**

### **Technologies Used**
- **.NET 8** (ASP.NET Core Web API)
- **PostgreSQL 16** with streaming replication
- **Docker Compose** for orchestration
- **ELK Stack** (Elasticsearch, Logstash, Kibana) for monitoring
- **Filebeat** for log collection

### **Project Structure**
```
StrongDatabase/
│
├── api/                        # .NET 8 API application
│   └── StrongDatabase.Api/     # Main API project
│
├── database/                   # Database configurations
│   ├── init-schema.sql         # Database schema and sample data
│   ├── pg_hba.conf            # PostgreSQL authentication
│   ├── postgres-primary.conf  # Primary database config
│   ├── postgres-replica.conf  # Replica databases config
│   └── setup-replica.sh       # Replication setup script
│
├── deployment/                 # Deployment scripts
│   ├── setup.sh               # Linux/macOS deployment
│   └── setup.bat              # Windows deployment
│
├── monitoring/                 # ELK Stack configuration
│   ├── dashboards/            # Kibana dashboards
│   ├── filebeat/              # Log collection config
│   └── logstash/              # Log processing config
│
└── docker-compose.yml         # Container orchestration
```

---

## 🎯 **Project Goals**

- Demonstrate **real PostgreSQL streaming replication** (not fake separate databases)
- **Automatic and secure failover** with zero data loss
- **Distributed read operations** across replicas
- **Intelligent load balancing** in the application layer
- **Comprehensive monitoring** with ELK Stack
- **Production-ready example** of .NET + PostgreSQL + Docker integration

---

## 🔄 **Replication Architecture**

### **Database Roles:**
- **primary-db**: Main database (all writes + emergency reads)
- **standby-db**: Synchronous standby (failover target, zero data loss)
- **replica1-db/replica2-db**: Asynchronous replicas (distributed reading)
- **strongdatabase-api**: .NET 8 API with automatic load balancing and failover

### **How Replication Works:**

#### 🔄 **Streaming Replication Process:**
1. **Primary** uses WAL (Write-Ahead Log) to record all changes
2. **WAL streaming** sends changes to all replicas in real-time
3. **Standby** receives changes synchronously (waits for confirmation)
4. **Replicas** receive changes asynchronously (eventual consistency)
5. **Hot standby** allows read queries on all replicas

#### 🎯 **Load Balancing Strategy:**
- **Writes**: Always go to primary database
- **Reads**: Distributed across replicas (round-robin)  
- **Failover**: If primary fails, standby becomes new primary
- **Health monitoring**: Automatic detection of failed nodes

#### 🔒 **Data Consistency:**
- **Synchronous replication** to standby (zero data loss)
- **Asynchronous replication** to read replicas (eventual consistency)
- **Same database/schema** across all nodes
- **Streaming replication** ensures real-time updates

---

## 🔍 **Understanding PostgreSQL Replication**

### **Synchronous Replication (Primary → Standby)**
- Primary waits for standby confirmation before committing
- **Zero data loss** guarantee
- Higher latency but total consistency
- Perfect for critical failover scenarios

### **Asynchronous Replication (Primary → Replicas)**
- Primary doesn't wait for replica confirmation
- **Lower latency** for write operations
- Eventual consistency (may lag a few seconds)
- Ideal for scaling read operations

### **Hot Standby**
- All replicas can serve read queries
- Replicas apply WAL changes while serving reads
- Automatic load distribution across healthy replicas

---

## 🚀 **Deployment Architecture**

### **Simplified Deployment Process:**
1. **Single command** starts entire infrastructure
2. **Automatic health checks** ensure proper startup order
3. **Real streaming replication** setup via `pg_basebackup`
4. **Zero configuration drift** - all replicas are exact copies

### **Container Dependencies:**
```
primary-db → standby-db → replica1-db → replica2-db → api → monitoring
```

### **What Makes This Architecture Special:**
- ✅ **80% fewer configuration files** than typical setups
- ✅ **Real PostgreSQL replication** (not separate databases)
- ✅ **Production-ready patterns** and best practices
- ✅ **Comprehensive monitoring** out of the box
- ✅ **Single source of truth** for schema and data

---

## 📊 **Monitoring & Observability**

### **Health Monitoring:**
- Detailed health checks for all database nodes
- API health endpoint with comprehensive status
- Automatic detection of replication lag

### **Log Aggregation:**
- Centralized logging via ELK Stack
- Real-time log analysis and visualization  
- Performance metrics and error tracking

### **Key Metrics Tracked:**
- Database connection health
- Replication lag (primary → replicas)
- API response times
- Error rates and patterns

---

## 🎓 **Educational Value**

This project demonstrates:
- **Real-world PostgreSQL replication** patterns
- **Production-ready Docker Compose** setups
- **.NET database connection management** and load balancing
- **Infrastructure as Code** principles
- **Comprehensive monitoring** implementation
- **Proper failover** and high availability design

Perfect for learning distributed database architecture, .NET integration with PostgreSQL, and Docker-based infrastructure deployment.

---

## 📝 **API Endpoints**

### **Customer Management:**
- `GET /api/customer` - List all customers
- `GET /api/customer/{id}` - Get customer by ID
- `POST /api/customer` - Create new customer
- `PUT /api/customer/{id}` - Update customer
- `DELETE /api/customer/{id}` - Delete customer

### **Product Management:**
- `GET /api/product` - List all products  
- `GET /api/product/{id}` - Get product by ID
- `POST /api/product` - Create new product
- `PUT /api/product/{id}` - Update product
- `DELETE /api/product/{id}` - Delete product

### **Order Management:**
- `GET /api/order` - List all orders
- `GET /api/order/{id}` - Get order by ID  
- `POST /api/order` - Create new order
- `PUT /api/order/{id}` - Update order
- `DELETE /api/order/{id}` - Delete order

### **System Health:**
- `GET /health` - Comprehensive system health check

---

## 🔧 **Development**

### **Requirements:**
- Docker & Docker Compose
- .NET 8 SDK (for local development)
- PostgreSQL client tools (optional, for debugging)

### **Local Development:**
```bash
# Start infrastructure
docker-compose up -d

# Run API locally (for debugging)
cd api/StrongDatabase.Api
dotnet run
```

### **Database Connection:**
```bash
# Connect to primary
psql -h localhost -p 5432 -U postgres -d strongdatabase

# Connect to replica
psql -h localhost -p 5433 -U postgres -d strongdatabase
```

---

## 📄 **License**

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Built with ❤️ for learning distributed database architecture** 