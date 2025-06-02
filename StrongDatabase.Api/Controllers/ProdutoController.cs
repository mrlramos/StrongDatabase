using Microsoft.AspNetCore.Mvc;
using StrongDatabase.Api.Models;
using StrongDatabase.Api.Services;

namespace StrongDatabase.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly Repository _repository;

        public ProductController(Repository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult<List<Product>>> GetProducts()
        {
            var products = await _repository.GetProductsAsync();
            return Ok(products);
        }

        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            var createdProduct = await _repository.AddProductAsync(product);
            return CreatedAtAction(nameof(GetProducts), new { id = createdProduct.Id }, createdProduct);
        }
    }
} 