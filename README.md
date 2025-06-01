# StrongDatabase

Projeto didático e moderno demonstrando arquitetura distribuída com .NET 8, PostgreSQL e Docker Compose, com replicação real, failover, e balanceamento.

---

## Tecnologias Utilizadas
- **.NET 8 (ASP.NET Core Web API)**
- **PostgreSQL 16** (Primary, Standby, Replica1, Replica2)
- **Docker Compose**

---

## Estrutura de Pastas
```
StrongDatabase/
│
├── StrongDatabase.Api/         # Código-fonte da aplicação .NET
├── docker/                       # Configurações e scripts dos bancos
│   ├── primary/                  # Config do banco primário
│   ├── standby/                  # Config do standby (síncrono)
│   ├── replica1/                 # Config da réplica 1 (assíncrona)
│   └── replica2/                 # Config da réplica 2 (assíncrona)
├── scripts/                      # Scripts SQL de criação dos bancos e dados
├── docker-compose.yml            # Orquestração dos containers
└── README.md                     # Documentação
```

---

## Objetivo do Projeto
- Demonstrar **arquitetura de bancos distribuídos** com replicação real (síncrona e assíncrona)
- **Failover automático** e seguro (zero perda de dados)
- **Leitura distribuída** entre réplicas
- **Balanceamento inteligente** de conexões na aplicação
- **Health checks** e monitoramento
- **Exemplo prático** de integração .NET + PostgreSQL + Docker Compose

---

## Arquitetura e Fluxo

### 1. **Containers e Funções**
- **primary-db**: Banco principal (escrita e leitura de emergência)
- **standby-db**: Standby síncrono (failover, nunca perde dados)
- **replica1-db/replica2-db**: Réplicas assíncronas (leitura distribuída)
- **strongdatabase-api**: API .NET 8, faz balanceamento e failover automático

### 2. **Replicação e Failover**
- O **primário** replica via WAL para standby (síncrono) e réplicas (assíncronas)
- O **standby** só confirma a escrita quando recebe o dado (garante zero perda)
- As **réplicas** recebem as alterações de forma assíncrona (podem atrasar alguns segundos)
- Se o primário cair, a aplicação redireciona escritas para o standby
- Se todas as réplicas e o primário caírem, leituras vão para o standby

### 3. **Balanceamento Inteligente (DbContextRouter)**
- **Escrita:** Sempre tenta o primário, se falhar, usa o standby
- **Leitura:** Distribui entre as réplicas, se todas falharem tenta o primário, se falhar, standby
- **Logs informativos** mostram cada fallback e decisão

### 4. **Health Checks**
- Endpoint `/health` monitora API e conexão com banco
- Pode ser usado por orquestradores ou load balancers externos

---

## Modelo de Dados

### Tabelas
- **Cliente**: `id`, `nome`, `email`
- **Produto**: `id`, `nome`, `preco`
- **Compra**: `id`, `cliente_id`, `produto_id`, `quantidade`, `data_compra`

### Exemplo de Dados
```sql
INSERT INTO cliente (nome, email) VALUES
  ('João Silva', 'joao@email.com'),
  ('Maria Souza', 'maria@email.com');
INSERT INTO produto (nome, preco) VALUES
  ('Notebook', 3500.00),
  ('Mouse', 80.00);
INSERT INTO compra (cliente_id, produto_id, quantidade) VALUES
  (1, 1, 1),
  (2, 2, 2);
```

---

## Endpoints Disponíveis

### Clientes (`/api/cliente`)
- `GET /api/cliente` — Lista todos os clientes
- `GET /api/cliente/{id}` — Busca cliente por ID
- `POST /api/cliente` — Cria novo cliente
- `PUT /api/cliente/{id}` — Atualiza cliente
- `DELETE /api/cliente/{id}` — Remove cliente

### Produtos (`/api/produto`)
- `GET /api/produto` — Lista todos os produtos
- `GET /api/produto/{id}` — Busca produto por ID
- `POST /api/produto` — Cria novo produto
- `PUT /api/produto/{id}` — Atualiza produto
- `DELETE /api/produto/{id}` — Remove produto

### Compras (`/api/compra`)
- `GET /api/compra` — Lista todas as compras
- `GET /api/compra/{id}` — Busca compra por ID (inclui cliente e produto)
- `POST /api/compra` — Cria nova compra
- `PUT /api/compra/{id}` — Atualiza compra
- `DELETE /api/compra/{id}` — Remove compra

### Health Check
- `GET /health` — Verifica saúde da API e conexão com banco

---

## Como Rodar o Projeto

1. **Suba os containers:**
   ```sh
   docker-compose up --build -d
   ```
2. **Acesse a API:**
   - [http://localhost:5000/swagger](http://localhost:5000/swagger) (interface interativa)
   - Ou use os endpoints REST diretamente

---

## Detalhes Técnicos e Scripts

### Configuração do Primário (`docker/primary/postgresql.conf`)
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

### Standby Síncrono (`docker/standby/standby-entrypoint.sh`)
- Clona dados do primário ao iniciar
- Conecta como standby síncrono
- Só confirma escrita quando recebe o dado

### Réplicas Assíncronas (`docker/replica1/replica-entrypoint.sh`)
- Clonam dados do primário ao iniciar
- Operam como hot standby assíncrono

### Orquestração Docker Compose
- Cada banco expõe uma porta diferente (`5433`, `5434`, `5435`, `5436`)
- API exposta em `5000`
- Volumes montam scripts e configs customizados
- Dependências garantem ordem de inicialização

---

## Fluxo de Balanceamento e Failover (Didático)

1. **Leitura**
   - API tenta ler nas réplicas (round-robin)
   - Se todas falharem, tenta o primário
   - Se primário falhar, tenta o standby
2. **Escrita**
   - API sempre tenta o primário
   - Se primário falhar, usa o standby
3. **Failover**
   - Se o primário cair, standby assume sem perda de dados
   - Réplicas podem ficar alguns segundos atrás (eventual consistency)

---

## Exemplos de Teste

### Testar Health Check
```sh
curl http://localhost:5000/health
```

### Listar Clientes
```sh
curl http://localhost:5000/api/cliente
```

### Criar Cliente
```sh
curl -X POST http://localhost:5000/api/cliente -H "Content-Type: application/json" -d '{"nome":"Novo Cliente","email":"novo@email.com"}'
```

### Simular Failover
1. Pare o primário:
   ```sh
   docker stop primary-db
   ```
2. Faça uma escrita (POST): a API irá redirecionar para o standby automaticamente.
3. Logs mostrarão o fallback.

---

## Observações Didáticas
- **Standby síncrono** garante zero perda de dados
- **Read replicas** aumentam performance de leitura
- **Balanceamento e failover** são automáticos e transparentes para o usuário
- **Arquitetura pronta para produção** (com adaptações de segurança e monitoramento)

---

## Créditos e Referências
- Projeto didático inspirado nas melhores práticas de arquitetura distribuída
- Documentação oficial: [PostgreSQL Streaming Replication](https://www.postgresql.org/docs/current/warm-standby.html)
- [ASP.NET Core Docs](https://learn.microsoft.com/aspnet/core)

---

## Como Limpar o Projeto

Para manter o projeto limpo e sem arquivos desnecessários, remova as seguintes pastas sempre que quiser:

- `StrongDatabase.Api/bin/`
- `StrongDatabase.Api/obj/`
- `.vs/` (cache do Visual Studio, pode estar em uso — feche o Visual Studio para apagar tudo)

Essas pastas são geradas automaticamente durante o build e podem ser excluídas sem risco. O comando para Windows PowerShell é:

```powershell
Remove-Item -Recurse -Force .\StrongDatabase.Api\bin
Remove-Item -Recurse -Force .\StrongDatabase.Api\obj
Remove-Item -Recurse -Force .\.vs
```

No Linux/Mac:
```bash
rm -rf StrongDatabase.Api/bin StrongDatabase.Api/obj .vs
```

> **Dica:** Antes de subir para o Git, sempre limpe o projeto para evitar arquivos desnecessários no repositório!

Essas práticas ajudam a manter o repositório enxuto e organizado.

---

## Subindo o ambiente do zero (após limpar tudo no Docker)

Sempre que você limpar todos os containers, volumes e imagens no Docker Desktop, siga este fluxo para garantir que a replicação funcione:

1. Suba normalmente:
   ```sh
   docker-compose up --build -d
   ```
   (Espere todos os containers subirem. As réplicas e standby podem parar na primeira tentativa, isso é esperado.)

2. Execute o script pós-up para ajustar a replicação:
   - **No Windows:**
     ```sh
     scripts\pos-up-windows.bat
     ```
   - **No Linux/Mac:**
     ```sh
     bash scripts/pos-up-linux.sh
     ```

Esses scripts vão:
- Copiar o `pg_hba.conf` customizado para dentro do primary-db
- Reiniciar o primary-db
- Reiniciar as réplicas e standby para garantir a replicação

Pronto! O ambiente estará normalizado e funcional.

---

**Dúvidas, sugestões ou quer expandir? Fique à vontade para contribuir!** 