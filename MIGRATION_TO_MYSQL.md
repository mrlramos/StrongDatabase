# Migração de PostgreSQL para MySQL - StrongDatabase

## 📋 Resumo das Alterações

Este documento detalha as alterações realizadas para migrar o projeto StrongDatabase de PostgreSQL para MySQL, mantendo a mesma arquitetura de alta disponibilidade com Primary, Standby e Replicas.

## 🔄 Alterações Realizadas

### 1. Docker Compose (`docker-compose.yml`)
- **Imagem**: Alterada de `postgres:16` para `mysql:8.0`
- **Portas**: 
  - Primary: `5433:5432` → `3307:3306`
  - Standby: `5434:5432` → `3308:3306`
  - Replica1: `5435:5432` → `3309:3306`
  - Replica2: `5436:5432` → `3310:3306`
- **Variáveis de ambiente**: Alteradas para padrão MySQL
- **Health checks**: Atualizados para `mysqladmin ping`
- **Configurações de replicação**: Adaptadas para MySQL GTID

### 2. Connection Strings
**Arquivos alterados:**
- `api/StrongDatabase.Api/appsettings.json`
- `api/StrongDatabase.Api/appsettings.Development.json`

**Formato anterior (PostgreSQL):**
```json
"DefaultConnection": "Host=primary-db;Port=5432;Database=strongdatabase;Username=postgres;Password=postgres_pass"
```

**Formato atual (MySQL):**
```json
"DefaultConnection": "Server=primary-db;Port=3306;Database=strongdatabase;Uid=mysql_user;Pwd=mysql_pass;CharSet=utf8mb4;"
```

### 3. Dependências NuGet (`StrongDatabase.Api.csproj`)
**Removidas:**
- `AspNetCore.HealthChecks.NpgSql`
- `Npgsql`
- `Npgsql.EntityFrameworkCore.PostgreSQL`

**Adicionadas:**
- `AspNetCore.HealthChecks.MySql`
- `MySql.Data`
- `Pomelo.EntityFrameworkCore.MySql`

### 4. Entity Framework Core (`Data/DbContextRouter.cs`)
- Alterado de `.UseNpgsql()` para `.UseMySql()`
- Configurado `ServerVersion.AutoDetect()` para compatibilidade
- Ajustados parâmetros de retry e timeout

### 5. Health Check Service (`Services/DatabaseHealthCheckService.cs`)
- Alterado de `NpgsqlConnection` para `MySqlConnection`
- Atualizadas queries SQL para sintaxe MySQL
- Modificado para usar `VERSION()`, `DATABASE()`, `USER()` e variáveis `@@`

### 6. Schema SQL (`database/init-schema.sql`)
**Principais alterações:**
- `\c strongdatabase;` → `USE strongdatabase;`
- `SERIAL` → `INT AUTO_INCREMENT`
- `NUMERIC(10,2)` → `DECIMAL(10,2)`
- `NOW()` → `CURRENT_TIMESTAMP`
- Adicionado `ENGINE=InnoDB` e `CHARSET=utf8mb4`
- Foreign Keys explícitas ao invés de `REFERENCES`

### 7. Arquivos de Configuração MySQL
**Criados:**
- `database/mysql-primary.cnf` - Configurações do servidor primary
- `database/mysql-replica.cnf` - Configurações das replicas
- `database/setup-mysql-replica.sh` - Script de configuração de replicação

**Removidos:**
- `database/pg_hba.conf`
- `database/postgres-primary.conf`
- `database/postgres-replica.conf`
- `database/setup-replica.sh`

## 🚀 Como Executar

### Pré-requisitos
1. Docker e Docker Compose instalados
2. .NET 8.0 SDK (para desenvolvimento local)

### Executar o Sistema
```bash
# Parar containers existentes
docker-compose down

# Rebuild e iniciar com MySQL
docker-compose up --build
```

### Verificar Status
- **API**: http://localhost:5000
- **Health Check**: http://localhost:5000/health
- **Health Check UI**: http://localhost:5000/health-ui
- **Swagger**: http://localhost:5000/swagger

### Conectar nos Bancos
```bash
# Primary Database
mysql -h localhost -P 3307 -u mysql_user -pmysql_pass strongdatabase

# Standby Database
mysql -h localhost -P 3308 -u mysql_user -pmysql_pass strongdatabase

# Replica 1
mysql -h localhost -P 3309 -u mysql_user -pmysql_pass strongdatabase

# Replica 2
mysql -h localhost -P 3310 -u mysql_user -pmysql_pass strongdatabase
```

## 🔧 Configurações Importantes

### Replicação MySQL
- **Modo GTID**: Habilitado para replicação automática
- **Binary Log**: ROW format para maior consistência
- **Read-only**: Configurado nas replicas e standby
- **Auto-positioning**: Simplifica configuração de replicação

### Performance
- **InnoDB Buffer Pool**: 256M por instância
- **Character Set**: UTF8MB4 para suporte completo Unicode
- **Connection Timeout**: 15 segundos
- **Retry Policy**: 2 tentativas com 3 segundos de delay

## ⚠️ Pontos de Atenção

1. **Portas**: As portas mudaram de 543x para 33xx
2. **Credenciais**: Usuário mudou de `postgres` para `mysql_user`
3. **Health Checks**: Agora usam `mysqladmin ping`
4. **Character Set**: Configurado para UTF8MB4 por padrão
5. **Engine**: Todas as tabelas usam InnoDB

## 🔍 Verificação de Funcionalidade

1. **API Health Check**: GET `/health`
2. **Database Connectivity**: Verificar logs dos containers
3. **Replication Status**: 
   ```sql
   SHOW SLAVE STATUS\G
   ```
4. **Data Consistency**: Inserir dados no primary e verificar nas replicas

## 🐛 Troubleshooting

### Container não inicia
```bash
docker-compose logs [service-name]
```

### Problemas de replicação
```bash
docker exec -it [replica-container] mysql -u root -p
mysql> SHOW SLAVE STATUS\G
```

### Problemas de conexão da API
- Verificar logs: `docker-compose logs api`
- Verificar health checks: `curl http://localhost:5000/health` 