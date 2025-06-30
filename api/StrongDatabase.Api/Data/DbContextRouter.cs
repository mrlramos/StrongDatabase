using Microsoft.EntityFrameworkCore;
using StrongDatabase.Api.Models;

namespace StrongDatabase.Api.Data
{
    /// <summary>
    /// Roteador inteligente de contextos de banco de dados
    /// Estratégia ADAPTATIVA:
    /// - ESCRITAS: PRIMARY (sempre) -> STANDBY (emergência se primary cair)
    /// - LEITURAS: REPLICAS disponíveis (load balancing) -> STANDBY -> PRIMARY (fallback)
    /// - Auto-detecta quais bancos estão realmente disponíveis
    /// </summary>
    public class DbContextRouter
    {
        private readonly IConfiguration _config;
        private readonly ILogger<DbContextRouter> _logger;
        private readonly string _primaryConnection;
        private readonly string _standbyConnection;
        private readonly string[] _replicaConnections;
        private int _replicaIndex = 0;
        private readonly object _lockObject = new object();
        
        // Cache de disponibilidade (atualizado periodicamente)
        private readonly Dictionary<string, bool> _availabilityCache = new();
        private DateTime _lastHealthCheck = DateTime.MinValue;
        private readonly TimeSpan _healthCheckInterval = TimeSpan.FromSeconds(30);

        public DbContextRouter(IConfiguration config, ILogger<DbContextRouter> logger)
        {
            _config = config;
            _logger = logger;
            _primaryConnection = _config.GetConnectionString("DefaultConnection")!;
            _standbyConnection = _config.GetConnectionString("Standby")!;
            _replicaConnections = new[]
            {
                _config.GetConnectionString("Replica1")!,
                _config.GetConnectionString("Replica2")!
            };
            
            _logger.LogInformation("🔧 [DbRouter] Configurado com estratégia ADAPTATIVA");
            _logger.LogInformation("🟢 [DbRouter] Primary: {Connection}", _primaryConnection);
            _logger.LogInformation("🔵 [DbRouter] Standby: {Connection}", _standbyConnection);
            _logger.LogInformation("🟡 [DbRouter] Replica1: {Connection}", _replicaConnections[0]);
            _logger.LogInformation("🟡 [DbRouter] Replica2: {Connection}", _replicaConnections[1]);
        }

        /// <summary>
        /// Obtém contexto para operações de ESCRITA
        /// </summary>
        public async Task<AppDbContext> GetWriteContextAsync()
        {
            _logger.LogInformation("✍️ [DbRouter] Solicitação de contexto para ESCRITA");
            
            await RefreshHealthCacheIfNeeded();
            
            // 1. Tentar PRIMARY primeiro (preferência)
            if (_availabilityCache.GetValueOrDefault("Primary", false))
            {
                try
                {
                    var primaryContext = CreateContext(_primaryConnection, "PRIMARY");
                    if (await TestConnectionAsync(primaryContext))
                    {
                        _logger.LogInformation("✅ [DbRouter] Usando PRIMARY para escrita");
                        return primaryContext;
                    }
                    primaryContext.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ [DbRouter] PRIMARY falhou para escrita: {Error}", ex.Message);
                }
            }

            // 2. Fallback para STANDBY (emergência)
            if (_availabilityCache.GetValueOrDefault("Standby", false))
            {
                try
                {
                    var standbyContext = CreateContext(_standbyConnection, "STANDBY");
                    if (await TestConnectionAsync(standbyContext))
                    {
                        _logger.LogWarning("🚨 [DbRouter] Usando STANDBY para escrita de EMERGÊNCIA!");
                        return standbyContext;
                    }
                    standbyContext.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [DbRouter] STANDBY também falhou: {Error}", ex.Message);
                }
            }

            _logger.LogCritical("💀 [DbRouter] TODOS os bancos de escrita indisponíveis!");
            throw new InvalidOperationException("Sistema de bancos de escrita completamente indisponível");
        }

        /// <summary>
        /// Obtém contexto para operações de LEITURA com balanceamento inteligente
        /// </summary>
        public async Task<AppDbContext> GetReadContextAsync()
        {
            _logger.LogInformation("📖 [DbRouter] Solicitação de contexto para LEITURA");

            await RefreshHealthCacheIfNeeded();

            // 1. Tentar REPLICAS primeiro (load balancing)
            var replicaContext = await TryGetAvailableReplicaAsync();
            if (replicaContext != null)
            {
                return replicaContext;
            }

            // 2. Fallback para STANDBY
            if (_availabilityCache.GetValueOrDefault("Standby", false))
            {
                try
                {
                    var standbyContext = CreateContext(_standbyConnection, "STANDBY");
                    if (await TestConnectionAsync(standbyContext))
                    {
                        _logger.LogInformation("🔵 [DbRouter] Usando STANDBY para leitura");
                        return standbyContext;
                    }
                    standbyContext.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ [DbRouter] STANDBY indisponível para leitura: {Error}", ex.Message);
                }
            }

            // 3. Último recurso: PRIMARY
            if (_availabilityCache.GetValueOrDefault("Primary", false))
            {
                try
                {
                    var primaryContext = CreateContext(_primaryConnection, "PRIMARY");
                    if (await TestConnectionAsync(primaryContext))
                    {
                        _logger.LogInformation("🟢 [DbRouter] Usando PRIMARY para leitura (último recurso)");
                        return primaryContext;
                    }
                    primaryContext.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [DbRouter] PRIMARY também indisponível: {Error}", ex.Message);
                }
            }

            _logger.LogCritical("💀 [DbRouter] TODOS os bancos indisponíveis!");
            throw new InvalidOperationException("Sistema de bancos completamente indisponível");
        }

        /// <summary>
        /// Métodos síncronos para compatibilidade
        /// </summary>
        public AppDbContext GetWriteContext()
        {
            return GetWriteContextAsync().GetAwaiter().GetResult();
        }

        public AppDbContext GetReadContext()
        {
            return GetReadContextAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Tenta obter uma replica disponível
        /// </summary>
        private async Task<AppDbContext?> TryGetAvailableReplicaAsync()
        {
            var availableReplicas = new List<int>();
            
            // Verificar quais replicas estão disponíveis
            for (int i = 0; i < _replicaConnections.Length; i++)
            {
                if (_availabilityCache.GetValueOrDefault($"Replica{i + 1}", false))
                {
                    availableReplicas.Add(i);
                }
            }

            if (availableReplicas.Count == 0)
            {
                _logger.LogWarning("🔴 [DbRouter] Nenhuma REPLICA disponível");
                return null;
            }

            // Load balancing round-robin nas replicas disponíveis
            for (int attempt = 0; attempt < availableReplicas.Count; attempt++)
            {
                int replicaIndex;
                lock (_lockObject)
                {
                    replicaIndex = availableReplicas[_replicaIndex % availableReplicas.Count];
                    _replicaIndex = (_replicaIndex + 1) % availableReplicas.Count;
                }

                var replicaConnection = _replicaConnections[replicaIndex];
                var replicaName = $"REPLICA{replicaIndex + 1}";

                try
                {
                    var context = CreateContext(replicaConnection, replicaName);
                    if (await TestConnectionAsync(context))
                    {
                        _logger.LogInformation("✅ [DbRouter] Usando {ReplicaName} para leitura", replicaName);
                        return context;
                    }
                    context.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ [DbRouter] {ReplicaName} falhou: {Error}", replicaName, ex.Message);
                    // Marcar como indisponível no cache
                    _availabilityCache[$"Replica{replicaIndex + 1}"] = false;
                }
            }

            return null;
        }

        /// <summary>
        /// Atualiza o cache de disponibilidade se necessário
        /// </summary>
        private async Task RefreshHealthCacheIfNeeded()
        {
            if (DateTime.UtcNow - _lastHealthCheck < _healthCheckInterval)
                return;

            _logger.LogDebug("🔄 [DbRouter] Atualizando cache de disponibilidade...");
            
            var healthStatus = await GetHealthStatusAsync();
            lock (_lockObject)
            {
                foreach (var (key, value) in healthStatus)
                {
                    _availabilityCache[key] = value;
                }
                _lastHealthCheck = DateTime.UtcNow;
            }

            var available = healthStatus.Count(x => x.Value);
            var total = healthStatus.Count;
            _logger.LogInformation("📊 [DbRouter] Status: {Available}/{Total} bancos disponíveis", available, total);
        }

        /// <summary>
        /// Cria um contexto otimizado
        /// </summary>
        private AppDbContext CreateContext(string connectionString, string databaseName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(15); // Timeout mais agressivo
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 2, maxRetryDelay: TimeSpan.FromSeconds(3), errorCodesToAdd: null);
                })
                .EnableSensitiveDataLogging(_config.GetValue<bool>("DetailedErrors", false))
                .EnableDetailedErrors(_config.GetValue<bool>("DetailedErrors", false))
                .Options;

            var context = new AppDbContext(options);
            context.Database.SetCommandTimeout(15);
            
            return context;
        }

        /// <summary>
        /// Testa conexão rapidamente
        /// </summary>
        private async Task<bool> TestConnectionAsync(AppDbContext context)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await context.Database.OpenConnectionAsync(cts.Token);
                await context.Database.CloseConnectionAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtém status de saúde completo
        /// </summary>
        public async Task<Dictionary<string, bool>> GetHealthStatusAsync()
        {
            var status = new Dictionary<string, bool>();
            var tasks = new List<Task>();

            // Testar todos os bancos em paralelo
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    using var context = CreateContext(_primaryConnection, "PRIMARY");
                    status["Primary"] = await TestConnectionAsync(context);
                }
                catch { status["Primary"] = false; }
            }));

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    using var context = CreateContext(_standbyConnection, "STANDBY");
                    status["Standby"] = await TestConnectionAsync(context);
                }
                catch { status["Standby"] = false; }
            }));

            for (int i = 0; i < _replicaConnections.Length; i++)
            {
                var index = i; // Capture for closure
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using var context = CreateContext(_replicaConnections[index], $"REPLICA{index + 1}");
                        status[$"Replica{index + 1}"] = await TestConnectionAsync(context);
                    }
                    catch { status[$"Replica{index + 1}"] = false; }
                }));
            }

            await Task.WhenAll(tasks);
            return status;
        }
    }
} 