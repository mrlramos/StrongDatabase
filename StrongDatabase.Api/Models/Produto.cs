namespace StrongDatabase.Api.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public ICollection<Order>? Orders { get; set; }
    }
} 