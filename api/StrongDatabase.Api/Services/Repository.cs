using StrongDatabase.Api.Data;
using StrongDatabase.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace StrongDatabase.Api.Services
{
    /// <summary>
    /// Central repository that performs intelligent routing of operations
    /// </summary>
    public class Repository
    {
        private readonly DbContextRouter _router;
        public Repository(DbContextRouter router)
        {
            _router = router;
        }

        // Read methods use replicas
        public async Task<List<Customer>> GetCustomersAsync() =>
            await _router.GetReadContext().Customers.AsNoTracking().ToListAsync();
        public async Task<List<Product>> GetProductsAsync() =>
            await _router.GetReadContext().Products.AsNoTracking().ToListAsync();
        public async Task<List<Order>> GetOrdersAsync() =>
            await _router.GetReadContext().Orders
                .Include(o => o.Customer)
                .Include(o => o.Product)
                .AsNoTracking().ToListAsync();

        // Write methods use primary
        public async Task<Customer> AddCustomerAsync(Customer customer)
        {
            using var ctx = _router.GetWriteContext();
            ctx.Customers.Add(customer);
            await ctx.SaveChangesAsync();
            return customer;
        }
        public async Task<Product> AddProductAsync(Product product)
        {
            using var ctx = _router.GetWriteContext();
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync();
            return product;
        }
        public async Task<Order> AddOrderAsync(Order order)
        {
            using var ctx = _router.GetWriteContext();
            ctx.Orders.Add(order);
            await ctx.SaveChangesAsync();
            return order;
        }
        // Update/delete methods can be implemented similarly
    }
} 