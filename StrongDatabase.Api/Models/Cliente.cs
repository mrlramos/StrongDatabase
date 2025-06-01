namespace StrongDatabase.Api.Models
{
    public class Cliente
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public ICollection<Compra>? Compras { get; set; }
    }
} 