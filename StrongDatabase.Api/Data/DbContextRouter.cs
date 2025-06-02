using Microsoft.EntityFrameworkCore;
using StrongDatabase.Api.Models;

namespace StrongDatabase.Api.Data
{
    /// <summary>
    /// Service responsible for providing the DbContext connected to the correct database
    /// - Write: always to primary
    /// - Read: tries replicas, fallback to primary
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
        /// Gets a context for write operations (always primary)
        /// </summary>
        public AppDbContext GetWriteContext()
        {
            try
            {
                _logger.LogInformation("[LoadBalancer] Using primary database for write operations");
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
                _logger.LogWarning(ex, "[Failover] Primary unavailable for write operations, redirecting to standby!");
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
        /// Gets a context for read operations (load balances between replicas, fallback to primary)
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
                    _logger.LogInformation($"[LoadBalancer] Using replica {idx + 1} for read operations");
                    return ctx;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Failed to connect to replica {idx + 1}, trying next...");
                }
            }
            // Fallback: primary
            try
            {
                _logger.LogWarning("[LoadBalancer] All replicas unavailable, trying primary for read operations");
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
                _logger.LogWarning(ex, "[Failover] Primary also unavailable for read operations, redirecting to standby!");
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