using Microsoft.AspNetCore.Mvc;
using StrongDatabase.Api.Models;
using StrongDatabase.Api.Services;

namespace StrongDatabase.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly Repository _repository;

        public OrderController(Repository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult<List<Order>>> GetOrders()
        {
            var orders = await _repository.GetOrdersAsync();
            return Ok(orders);
        }

        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(Order order)
        {
            var createdOrder = await _repository.AddOrderAsync(order);
            return CreatedAtAction(nameof(GetOrders), new { id = createdOrder.Id }, createdOrder);
        }
    }
} 