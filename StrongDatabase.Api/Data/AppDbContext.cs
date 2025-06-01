using Microsoft.EntityFrameworkCore;
using StrongDatabase.Api.Models;

namespace StrongDatabase.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Compra> Compras { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Cliente>(entity => {
                entity.ToTable("cliente");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Nome).HasColumnName("nome");
                entity.Property(e => e.Email).HasColumnName("email");
            });
            modelBuilder.Entity<Produto>(entity => {
                entity.ToTable("produto");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Nome).HasColumnName("nome");
                entity.Property(e => e.Preco).HasColumnName("preco");
            });
            modelBuilder.Entity<Compra>(entity => {
                entity.ToTable("compra");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ClienteId).HasColumnName("cliente_id");
                entity.Property(e => e.ProdutoId).HasColumnName("produto_id");
                entity.Property(e => e.Quantidade).HasColumnName("quantidade");
                entity.Property(e => e.DataCompra).HasColumnName("data_compra");
            });
        }
    }
} 