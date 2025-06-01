using StrongDatabase.Api.Data;
using StrongDatabase.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace StrongDatabase.Api.Services
{
    /// <summary>
    /// Repositório central que faz o roteamento inteligente de operações
    /// </summary>
    public class Repository
    {
        private readonly DbContextRouter _router;
        public Repository(DbContextRouter router)
        {
            _router = router;
        }

        // Métodos de leitura usam as réplicas
        public async Task<List<Cliente>> GetClientesAsync() =>
            await _router.GetReadContext().Clientes.AsNoTracking().ToListAsync();
        public async Task<List<Produto>> GetProdutosAsync() =>
            await _router.GetReadContext().Produtos.AsNoTracking().ToListAsync();
        public async Task<List<Compra>> GetComprasAsync() =>
            await _router.GetReadContext().Compras
                .Include(c => c.Cliente)
                .Include(c => c.Produto)
                .AsNoTracking().ToListAsync();

        // Métodos de escrita usam o primário
        public async Task<Cliente> AddClienteAsync(Cliente cliente)
        {
            using var ctx = _router.GetWriteContext();
            ctx.Clientes.Add(cliente);
            await ctx.SaveChangesAsync();
            return cliente;
        }
        public async Task<Produto> AddProdutoAsync(Produto produto)
        {
            using var ctx = _router.GetWriteContext();
            ctx.Produtos.Add(produto);
            await ctx.SaveChangesAsync();
            return produto;
        }
        public async Task<Compra> AddCompraAsync(Compra compra)
        {
            using var ctx = _router.GetWriteContext();
            ctx.Compras.Add(compra);
            await ctx.SaveChangesAsync();
            return compra;
        }
        // Métodos de update/delete podem ser implementados de forma similar
    }
} 