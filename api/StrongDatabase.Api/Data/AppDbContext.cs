using Microsoft.EntityFrameworkCore;
using StrongDatabase.Api.Models;

namespace StrongDatabase.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Customer>(entity => {
                entity.ToTable("cliente");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("nome");
                entity.Property(e => e.Email).HasColumnName("email");
            });
            
            modelBuilder.Entity<Product>(entity => {
                entity.ToTable("produto");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("nome");
                entity.Property(e => e.Price).HasColumnName("preco");
            });
            
            modelBuilder.Entity<Order>(entity => {
                entity.ToTable("compra");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CustomerId).HasColumnName("cliente_id");
                entity.Property(e => e.ProductId).HasColumnName("produto_id");
                entity.Property(e => e.Quantity).HasColumnName("quantidade");
                entity.Property(e => e.OrderDate).HasColumnName("data_compra");
            });
        }
    }
} 