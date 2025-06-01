using Microsoft.EntityFrameworkCore;
using StrongDatabase.Api.Models;

namespace StrongDatabase.Api.Data
{
    /// <summary>
    /// Serviço responsável por fornecer o DbContext conectado ao banco correto
    /// - Escrita: sempre no primário
    /// - Leitura: tenta as réplicas, fallback para primário
    /// </summary>
    public class DbContextRouter
    {
        private readonly IConfiguration _config;
        private readonly ILogger<DbContextRouter> _logger;
        private readonly string _primary;
        private readonly string _standby;
        private readonly string[] _replicas;
        private int _replicaIndex = 0;

        public DbContextRouter(IConfiguration config, ILogger<DbContextRouter> logger)
        {
            _config = config;
            _logger = logger;
            _primary = _config.GetConnectionString("DefaultConnection")!;
            _standby = _config.GetConnectionString("Standby")!;
            _replicas = new[]
            {
                _config.GetConnectionString("Replica1")!,
                _config.GetConnectionString("Replica2")!
            };
        }

        /// <summary>
        /// Obtém um contexto para operações de escrita (sempre primário)
        /// </summary>
        public AppDbContext GetWriteContext()
        {
            try
            {
                _logger.LogInformation("[LoadBalancer] Usando banco primário para escrita");
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseNpgsql(_primary)
                    .Options;
                var ctx = new AppDbContext(options);
                ctx.Database.OpenConnection();
                ctx.Database.CloseConnection();
                return ctx;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Failover] Primário indisponível para escrita, redirecionando para standby!");
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseNpgsql(_standby)
                    .Options;
                var ctx = new AppDbContext(options);
                ctx.Database.OpenConnection();
                ctx.Database.CloseConnection();
                return ctx;
            }
        }

        /// <summary>
        /// Obtém um contexto para operações de leitura (balanceia entre réplicas, fallback para primário)
        /// </summary>
        public AppDbContext GetReadContext()
        {
            for (int i = 0; i < _replicas.Length; i++)
            {
                var idx = (_replicaIndex + i) % _replicas.Length;
                try
                {
                    var options = new DbContextOptionsBuilder<AppDbContext>()
                        .UseNpgsql(_replicas[idx])
                        .Options;
                    var ctx = new AppDbContext(options);
                    ctx.Database.OpenConnection();
                    ctx.Database.CloseConnection();
                    _replicaIndex = (idx + 1) % _replicas.Length;
                    _logger.LogInformation($"[LoadBalancer] Usando réplica {idx + 1} para leitura");
                    return ctx;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Falha ao conectar na réplica {idx + 1}, tentando próxima...");
                }
            }
            // Fallback: primário
            try
            {
                _logger.LogWarning("[LoadBalancer] Todas as réplicas indisponíveis, tentando primário para leitura");
                var fallbackOptions = new DbContextOptionsBuilder<AppDbContext>()
                    .UseNpgsql(_primary)
                    .Options;
                var ctx = new AppDbContext(fallbackOptions);
                ctx.Database.OpenConnection();
                ctx.Database.CloseConnection();
                return ctx;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Failover] Primário também indisponível para leitura, redirecionando para standby!");
                var standbyOptions = new DbContextOptionsBuilder<AppDbContext>()
                    .UseNpgsql(_standby)
                    .Options;
                var ctx = new AppDbContext(standbyOptions);
                ctx.Database.OpenConnection();
                ctx.Database.CloseConnection();
                return ctx;
            }
        }
    }
} 